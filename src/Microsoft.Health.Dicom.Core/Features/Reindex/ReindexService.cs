﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    public class ReindexService : IReindexService
    {
        private readonly IReindexJobClient _reindexJobClient;
        private readonly IExtendedQueryTagJobStore _reindexJobTagStore;
        public ReindexService(IReindexJobClient reindexJobClient, IExtendedQueryTagJobStore reindexJobTagStore)
        {
            EnsureArg.IsNotNull(reindexJobClient);
            EnsureArg.IsNotNull(reindexJobTagStore);
            _reindexJobClient = reindexJobClient;
            _reindexJobTagStore = reindexJobTagStore;
        }

        public async Task RemoveTagFromReindexing(ExtendedQueryTagStoreEntry extendedQueryTagEntry, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(extendedQueryTagEntry);
            var row = await _reindexJobTagStore.GetExtendedQueryTagJobStoreEntryAsync(extendedQueryTagEntry.Key, cancellationToken);
            // TODO: what if fail to update job tag status?
            await _reindexJobTagStore.UpdateExtendedQueryTagJobStatus(row.JobId, extendedQueryTagEntry.Key, ExtendedQueryTagJobStatus.Paused, cancellationToken);
        }

        public Task<string> StartNewReindexJob(IEnumerable<ExtendedQueryTagStoreEntry> extendedQueryTagStoreEntries, CancellationToken cancellationToken = default)
        {
            return _reindexJobClient.StartNewReindexJob(extendedQueryTagStoreEntries, cancellationToken);
        }
    }
}