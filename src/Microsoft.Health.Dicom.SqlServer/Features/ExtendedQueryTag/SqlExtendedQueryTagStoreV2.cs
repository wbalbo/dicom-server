﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
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
    internal class SqlExtendedQueryTagStoreV2 : SqlExtendedQueryTagStoreV1
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<SqlExtendedQueryTagStoreV2> _logger;

        public SqlExtendedQueryTagStoreV2(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV2> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public override SchemaVersion Version => SchemaVersion.V2;

        public override async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries, int maxCount, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                IEnumerable<AddExtendedQueryTagsInputTableTypeV1Row> rows = extendedQueryTagEntries.Select(ToAddExtendedQueryTagsInputTableTypeV1Row);

                V2.AddExtendedQueryTags.PopulateCommand(sqlCommandWrapper, new V2.AddExtendedQueryTagsTableValuedParameters(rows));

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                    return Array.Empty<ExtendedQueryTagStoreEntry>();
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            throw new ExtendedQueryTagsAlreadyExistsException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public override async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(string path, CancellationToken cancellationToken = default)
        {
            List<ExtendedQueryTagStoreEntry> results = new List<ExtendedQueryTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetExtendedQueryTag.PopulateCommand(sqlCommandWrapper, path);

                var executionTimeWatch = Stopwatch.StartNew();
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (int tagKey, string tagPath, string tagVR, string tagPrivateCreator, int tagLevel, int tagStatus) = reader.ReadRow(
                            VLatest.ExtendedQueryTag.TagKey,
                            VLatest.ExtendedQueryTag.TagPath,
                            VLatest.ExtendedQueryTag.TagVR,
                            VLatest.ExtendedQueryTag.TagPrivateCreator,
                            VLatest.ExtendedQueryTag.TagLevel,
                            VLatest.ExtendedQueryTag.TagStatus);

                        results.Add(new ExtendedQueryTagStoreEntry(tagKey, tagPath, tagVR, tagPrivateCreator, (QueryTagLevel)tagLevel, (ExtendedQueryTagStatus)tagStatus));
                    }

                    executionTimeWatch.Stop();
                    _logger.LogInformation(executionTimeWatch.ElapsedMilliseconds.ToString());
                }
            }

            return results;
        }

        internal static AddExtendedQueryTagsInputTableTypeV1Row ToAddExtendedQueryTagsInputTableTypeV1Row(AddExtendedQueryTagEntry entry)
        {
            return new AddExtendedQueryTagsInputTableTypeV1Row(entry.Path, entry.VR, entry.PrivateCreator, (byte)((QueryTagLevel)Enum.Parse(typeof(QueryTagLevel), entry.Level, true)));
        }

        public override async Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteExtendedQueryTag.PopulateCommand(sqlCommandWrapper, tagPath, (byte)ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[vr]);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new ExtendedQueryTagNotFoundException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagNotFound, tagPath));
                        case SqlErrorCodes.PreconditionFailed:
                            throw new ExtendedQueryTagBusyException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagIsBusy, tagPath));
                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }
    }
}
