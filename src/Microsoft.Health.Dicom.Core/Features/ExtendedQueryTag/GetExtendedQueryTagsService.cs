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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagsService : IGetExtendedQueryTagsService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IUrlResolver _urlResolver;

        public GetExtendedQueryTagsService(
            IExtendedQueryTagStore extendedQueryTagStore,
            IDicomTagParser dicomTagParser,
            IUrlResolver urlResolver)
        {
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _dicomTagParser = EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        }

        public async Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            DicomTag[] tags;
            string numericalTagPath;
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

            ExtendedQueryTagStoreJoinEntry extendedQueryTag = await _extendedQueryTagStore.GetExtendedQueryTagAsync(numericalTagPath, cancellationToken);
            return new GetExtendedQueryTagResponse(extendedQueryTag.ToGetExtendedQueryTagEntry(_urlResolver));
        }

        public async Task<GetExtendedQueryTagsResponse> GetExtendedQueryTagsAsync(int limit, int offset = 0, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ExtendedQueryTagStoreJoinEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(limit, offset, cancellationToken);
            return new GetExtendedQueryTagsResponse(extendedQueryTags.Select(x => x.ToGetExtendedQueryTagEntry(_urlResolver)));
        }
    }
}
