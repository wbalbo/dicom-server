﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 4.
    /// </summary>
    internal class SqlIndexDataStoreV4 : SqlIndexDataStoreV3
    {
        public SqlIndexDataStoreV4(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        public override async Task<long> CreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, ExtendedQueryTagVersion version, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags.Where(tag => tag.IsExtendedQueryTag));
                VLatest.AddInstanceTableValuedParameters parameters = new VLatest.AddInstanceTableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeRows,
                    rows.PersonNameRows
                );

                VLatest.AddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientName),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                    instance.GetStringDateAsDate(DicomTag.StudyDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.StudyDescription),
                    instance.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber),
                    instance.GetSingleValueOrDefault<string>(DicomTag.Modality),
                    instance.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                    instance.GetStringDateAsDate(DicomTag.PatientBirthDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName),
                    (byte)IndexStatus.Creating,
                    version.GetVersion(),
                    parameters);

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlErrorCodes.Conflict)
                    {
                        throw ex.State switch
                        {
                             1=> new PendingInstanceException()
                        };
                        switch (ex.State)
                        {
                            default:
                                break;
                        }
                        if (ex.State == 2)
                        {
                            throw new PendingInstanceException();
                        }

                        throw new InstanceAlreadyExistsException();
                    }

                    throw new DataStoreException(ex);
                }
            }
        }

        public override async Task ReindexInstanceAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags.Where(tag => tag.IsExtendedQueryTag));
                VLatest.ReindexInstanceTableValuedParameters parameters = new VLatest.ReindexInstanceTableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeRows,
                    rows.PersonNameRows
                );

                VLatest.ReindexInstance.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict => new PendingInstanceException(),
                        _ => new DataStoreException(ex),
                    };

                }
            }
        }
    }
}
