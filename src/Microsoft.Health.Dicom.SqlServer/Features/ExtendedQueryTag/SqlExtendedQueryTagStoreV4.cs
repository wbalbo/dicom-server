// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    // TODO: don't need such do-nothing class
    internal class SqlExtendedQueryTagStoreV4 : SqlExtendedQueryTagStoreV3
    {

        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlExtendedQueryTagStoreV4(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV4> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
            _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        public override SchemaVersion Version => SchemaVersion.V4;
        public override async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries, int maxAllowedCount, CancellationToken cancellationToken)
        {
            List<ExtendedQueryTagStoreEntry> result = new List<ExtendedQueryTagStoreEntry>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                IEnumerable<AddExtendedQueryTagsInputTableTypeV1Row> rows = extendedQueryTagEntries.Select(ToAddExtendedQueryTagsInputTableTypeV1Row);

                VLatest.AddExtendedQueryTags.PopulateCommand(sqlCommandWrapper, maxAllowedCount, new VLatest.AddExtendedQueryTagsTableValuedParameters(rows));

                try
                {
                    using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (
                            int rTagKey,
                            string rTagPath,
                            string rTagVR,
                            string rTagPrivateCreator,
                            byte rTagLevel,
                            byte rTagStatus
                        ) = reader.ReadRow(
                           VLatest.ExtendedQueryTag.TagKey,
                           VLatest.ExtendedQueryTag.TagPath,
                           VLatest.ExtendedQueryTag.TagVR,
                           VLatest.ExtendedQueryTag.TagPrivateCreator,
                           VLatest.ExtendedQueryTag.TagLevel,
                           VLatest.ExtendedQueryTag.TagStatus);
                        result.Add(new ExtendedQueryTagStoreEntry(rTagKey, rTagPath, rTagVR, rTagPrivateCreator, (QueryTagLevel)rTagLevel, (ExtendedQueryTagStatus)rTagStatus));
                    }
                    return result;
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                        {
                            if (ex.State == 1)
                            {
                                throw new ExtendedQueryTagsExceedsMaxAllowedCountException(maxAllowedCount);
                            }
                            else
                            {
                                throw new ExtendedQueryTagsAlreadyExistsException();
                            }
                        }

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }
    }
}
