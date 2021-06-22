// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 2.
    /// </summary>
    internal class SqlIndexDataStoreV4 : SqlIndexDataStoreV3
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlIndexDataStoreV4(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        public override async Task ReindexInstanceAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                // Build parameter for extended query tag.
                var parameters = AddInstanceTableValuedParametersBuilder.BuildReindex(
                    instance,
                    queryTags.Where(tag => tag.IsExtendedQueryTag));

                VLatest.ReindexInstance.PopulateCommand(
                sqlCommandWrapper,
                instance.GetString(DicomTag.StudyInstanceUID),
                instance.GetString(DicomTag.SeriesInstanceUID),
                instance.GetString(DicomTag.SOPInstanceUID),
                parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {

                    throw new DataStoreException(ex);
                }
            }
        }
    }
}
