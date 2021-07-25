// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagErrorsService : IAddExtendedQueryTagErrorsService
    {
        private readonly IStoreFactory<IExtendedQueryTagErrorStore> _extendedQueryTagStoreFactory;

        public AddExtendedQueryTagErrorsService(IStoreFactory<IExtendedQueryTagErrorStore> extendedQueryTagStoreFactory)
        {
            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
        }

        public async Task<long> AddExtendedQueryTagErrorAsync(int tagKey, DateTime createdTime, int errorCode, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, Int64 sopInstanceKey, CancellationToken cancellationToken = default)
        {
            IExtendedQueryTagErrorStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync(cancellationToken);

            return await extendedQueryTagStore.AddExtendedQueryTagErrorAsync(
                tagKey,
                createdTime,
                errorCode,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                sopInstanceKey,
                cancellationToken);
        }
    }
}
