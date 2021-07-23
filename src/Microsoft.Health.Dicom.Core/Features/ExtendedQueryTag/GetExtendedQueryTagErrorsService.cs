// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsService : IGetExtendedQueryTagErrorsService
    {
        private readonly IStoreFactory<IExtendedQueryTagErrorStore> _extendedQueryTagStoreFactory;
        private readonly IDicomTagParser _dicomTagParser;
        public GetExtendedQueryTagErrorsService(IStoreFactory<IExtendedQueryTagErrorStore> extendedQueryTagStoreFactory, IDicomTagParser dicomTagParser)
        {
            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
            _dicomTagParser = EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
        }

        public async Task<GetExtendedQueryTagErrorsResponse> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            string numericalTagPath = null;
            DicomTag[] tags;
            if (_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                if (tags.Length > 1)
                {
                    throw new NotImplementedException(DicomCoreResource.SequentialDicomTagsNotSupported);
                }

                numericalTagPath = tags[0].GetPath();
            }
            else
            {
                throw new InvalidExtendedQueryTagPathException(string.Format(DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            IExtendedQueryTagErrorStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync(cancellationToken);
            IReadOnlyList<ExtendedQueryTagError> extendedQueryTagErrors = await extendedQueryTagStore.GetExtendedQueryTagErrorsAsync(numericalTagPath, cancellationToken);

            return new GetExtendedQueryTagErrorsResponse(extendedQueryTagErrors);
        }
    }
}
