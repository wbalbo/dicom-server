// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Indexing
{
    internal class SqlReindexStore : IReindexStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<SqlReindexStore> _logger;

        public SqlReindexStore(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<SqlReindexStore> logger)
        {
            _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }
        public Task CompleteReindexAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ReindexEntry>> GetReindexEntriesAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ReindexOperation> PrepareReindexingAsync(IReadOnlyList<int> tagKeys, string operationId, CancellationToken cancellationToken = default)
        {
            // add ReindexEntry for each tagKey with Processing status
            EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
            EnsureArg.IsNotEmptyOrWhiteSpace(operationId, nameof(operationId));
            // TODO: if tagKeys is empty, throw exception?

            ReindexOperation result = new ReindexOperation();
            result.OperationId = operationId;
            List<ExtendedQueryTagStoreEntry> storeEntries = new List<ExtendedQueryTagStoreEntry>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.PrepareReindexing.PopulateCommand(sqlCommandWrapper, tagKeys.Select(x => new PrepareReindexingTableTypeV1Row(x)), operationId);

                using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    (
                        int rTagKey,
                        string rTagPath,
                        string rTagVR,
                        string rTagPrivateCreator,
                        byte rTagLevel,
                        byte rTagStatus,
                        string rOperationid,
                        byte rReindexStatus,
                        long? rStartWatermark,
                        long? rEndWatermark
                    ) = reader.ReadRow(
                       VLatest.ExtendedQueryTag.TagKey,
                       VLatest.ExtendedQueryTag.TagPath,
                       VLatest.ExtendedQueryTag.TagVR,
                       VLatest.ExtendedQueryTag.TagPrivateCreator,
                       VLatest.ExtendedQueryTag.TagLevel,
                       VLatest.ExtendedQueryTag.TagStatus,
                       VLatest.ReindexStore.OperationId,
                       VLatest.ReindexStore.ReindexStatus,
                       VLatest.ReindexStore.StartWatermark,
                       VLatest.ReindexStore.EndWatermark);
                    storeEntries.Add(new ExtendedQueryTagStoreEntry(rTagKey, rTagPath, rTagVR, rTagPrivateCreator, (QueryTagLevel)rTagLevel, (ExtendedQueryTagStatus)rTagStatus));
                    result.StartWatermark = rStartWatermark;
                    result.EndWatermark = rEndWatermark;
                }
            }
            result.StoreEntries = storeEntries;

            return result;
        }

        public Task UpdateReindexProgressAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
