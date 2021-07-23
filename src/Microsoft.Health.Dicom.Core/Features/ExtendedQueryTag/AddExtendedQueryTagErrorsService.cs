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
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;

        public AddExtendedQueryTagErrorsService(IStoreFactory<IExtendedQueryTagStore> extendedQueryTagStoreFactory)
        {
            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
        }

        public async Task<int> AddExtendedQueryTagErrorAsync(int tagKey, DateTime createdTime, int errorCode, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long sopInstanceKey, CancellationToken cancellationToken = default)
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync(cancellationToken);

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
