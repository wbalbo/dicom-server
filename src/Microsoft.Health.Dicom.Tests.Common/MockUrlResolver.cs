﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public class MockUrlResolver : IUrlResolver
    {
        public Uri ResolveOperationStatusUri(Guid operationId)
        {
            return new Uri("/" + OperationId.ToString(operationId), UriKind.Relative);
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagUri(string tagPath)
        {
            EnsureArg.IsNotNull(tagPath, nameof(tagPath));

            return new Uri("/" + tagPath, UriKind.Relative);
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagErrorsUri(string tagPath)
        {
            EnsureArg.IsNotNull(tagPath, nameof(tagPath));

            return new Uri("/" + tagPath + "/errors", UriKind.Relative);
        }

        public Uri ResolveRetrieveInstanceUri(InstanceIdentifier instanceIdentifier)
        {
            EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

            return new Uri(
                $"/{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}",
                UriKind.Relative);
        }

        public Uri ResolveRetrieveStudyUri(string studyInstanceUid)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return new Uri(studyInstanceUid, UriKind.Relative);
        }
    }
}
