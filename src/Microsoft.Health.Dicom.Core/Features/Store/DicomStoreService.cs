﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Event;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using DicomValidationException = Dicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public class DicomStoreService : IDicomStoreService
    {
        private static readonly Action<ILogger, int, ushort, Exception> LogValidationFailedDelegate =
            LoggerMessage.Define<int, ushort>(
                LogLevel.Information,
                default,
                "Validation failed for the DICOM instance entry at index '{DicomInstanceEntryIndex}'. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, int, ushort, Exception> LogFailedToStoreDelegate =
            LoggerMessage.Define<int, ushort>(
                LogLevel.Warning,
                default,
                "Failed to store the DICOM instance entry at index '{DicomInstanceEntryIndex}'. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, int, Exception> LogSuccessfullyStoredDelegate =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                default,
                "Successfully stored the DICOM instance entry at index '{DicomInstanceEntryIndex}'.");

        private static readonly Action<ILogger, int, Exception> LogFailedToDisposeDelegate =
            LoggerMessage.Define<int>(
                LogLevel.Warning,
                default,
                "Failed to dispose the DICOM instance entry at index '{DicomInstanceEntryIndex}'.");

        private readonly IDicomStoreResponseBuilder _dicomStoreResponseBuilder;
        private readonly IDicomDatasetMinimumRequirementValidator _dicomDatasetMinimumRequirementValidator;
        private readonly IDicomStoreOrchestrator _dicomStoreOrchestrator;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IUrlResolver _urlResolver;
        private IReadOnlyList<IDicomInstanceEntry> _dicomInstanceEntries;
        private string _requiredStudyInstanceUid;
        private List<DicomInstanceIdentifier> _persistedInstances;

        public DicomStoreService(
            IDicomStoreResponseBuilder dicomStoreResponseBuilder,
            IDicomDatasetMinimumRequirementValidator dicomDatasetMinimumRequirementValidator,
            IDicomStoreOrchestrator dicomStoreOrchestrator,
            ILogger<DicomStoreService> logger,
            IMediator mediator,
            IUrlResolver urlResolver)
        {
            EnsureArg.IsNotNull(dicomStoreResponseBuilder, nameof(dicomStoreResponseBuilder));
            EnsureArg.IsNotNull(dicomDatasetMinimumRequirementValidator, nameof(dicomDatasetMinimumRequirementValidator));
            EnsureArg.IsNotNull(dicomStoreOrchestrator, nameof(dicomStoreOrchestrator));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

            _dicomStoreResponseBuilder = dicomStoreResponseBuilder;
            _dicomDatasetMinimumRequirementValidator = dicomDatasetMinimumRequirementValidator;
            _dicomStoreOrchestrator = dicomStoreOrchestrator;
            _logger = logger;
            _mediator = mediator;
            _urlResolver = urlResolver;
            _persistedInstances = new List<DicomInstanceIdentifier>();
        }

        /// <inheritdoc />
        public async Task<DicomStoreResponse> ProcessAsync(
            IReadOnlyList<IDicomInstanceEntry> dicomInstanceEntries,
            string requiredStudyInstanceUid,
            CancellationToken cancellationToken)
        {
            if (dicomInstanceEntries != null)
            {
                _dicomInstanceEntries = dicomInstanceEntries;
                _requiredStudyInstanceUid = requiredStudyInstanceUid;

                for (int index = 0; index < dicomInstanceEntries.Count; index++)
                {
                    try
                    {
                        await ProcessDicomInstanceEntryAsync(index, cancellationToken);
                    }
                    finally
                    {
                        // Fire and forget.
                        int capturedIndex = index;

                        _ = Task.Run(() => DisposeResourceAsync(capturedIndex));
                    }
                }
            }

            if (_persistedInstances.Any())
            {
                await _mediator.Publish(
                    new DicomEventNotificationCollection(
                        _persistedInstances.Select(instanceId =>
                                                    new DicomEventNotification(
                                                        DicomEventType.DicomInstanceCreated,
                                                        new DicomInstanceCreatedEventData()
                                                        {
                                                            StudyInstanceUid = instanceId.StudyInstanceUid,
                                                            SeriesInstanceUid = instanceId.SeriesInstanceUid,
                                                            SopInstanceUid = instanceId.SopInstanceUid,
                                                        },
                                                        _urlResolver.ResolveRetrieveInstanceUri(instanceId).ToString()))
                        .ToList()));
            }

            return _dicomStoreResponseBuilder.BuildResponse(requiredStudyInstanceUid);
        }

        private async Task ProcessDicomInstanceEntryAsync(int index, CancellationToken cancellationToken)
        {
            IDicomInstanceEntry dicomInstanceEntry = _dicomInstanceEntries[index];

            DicomDataset dicomDataset = null;

            try
            {
                // Open and validate the DICOM instance.
                dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

                _dicomDatasetMinimumRequirementValidator.Validate(dicomDataset, _requiredStudyInstanceUid);
            }
            catch (Exception ex)
            {
                ushort failureCode = DicomFailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomValidationException _:
                        failureCode = DicomFailureReasonCodes.ValidationFailure;
                        break;

                    case DicomDatasetValidationException dicomDatasetValidationException:
                        failureCode = dicomDatasetValidationException.FailureCode;
                        break;
                }

                LogValidationFailedDelegate(_logger, index, failureCode, ex);

                _dicomStoreResponseBuilder.AddFailure(dicomDataset, failureCode);

                return;
            }

            try
            {
                // Store the instance.
                await _dicomStoreOrchestrator.StoreDicomInstanceEntryAsync(
                    dicomInstanceEntry,
                    cancellationToken);

                LogSuccessfullyStoredDelegate(_logger, index, null);

                _dicomStoreResponseBuilder.AddSuccess(dicomDataset);

                _persistedInstances.Add(dicomDataset.ToDicomInstanceIdentifier());
            }
            catch (Exception ex)
            {
                ushort failureCode = DicomFailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomInstanceAlreadyExistsException _:
                        failureCode = DicomFailureReasonCodes.SopInstanceAlreadyExists;
                        break;
                }

                LogFailedToStoreDelegate(_logger, index, failureCode, ex);

                _dicomStoreResponseBuilder.AddFailure(dicomDataset, failureCode);
            }
        }

        private async Task DisposeResourceAsync(int index)
        {
            try
            {
                await _dicomInstanceEntries[index].DisposeAsync();
            }
            catch (Exception ex)
            {
                LogFailedToDisposeDelegate(_logger, index, ex);
            }
        }
    }
}
