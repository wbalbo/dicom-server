﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlIndexDataStoreTestHelper : IIndexDataStoreTestHelper
    {
        private readonly string _connectionString;

        public SqlIndexDataStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<StudyMetadata>> GetStudyMetadataAsync(string studyInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<StudyMetadata>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.Study.TableName}
                        WHERE {VLatest.Study.StudyInstanceUid} = @studyInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new StudyMetadata(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task<IReadOnlyList<SeriesMetadata>> GetSeriesMetadataAsync(string seriesInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<SeriesMetadata>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.Series.TableName}
                        WHERE {VLatest.Series.SeriesInstanceUid} = @seriesInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new SeriesMetadata(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task<IReadOnlyList<Instance>> GetInstancesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            var results = new List<Instance>();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.Instance.TableName}
                        WHERE {VLatest.Instance.StudyInstanceUid} = @studyInstanceUid
                            AND {VLatest.Instance.SeriesInstanceUid} = @seriesInstanceUid
                            AND {VLatest.Instance.SopInstanceUid} = @sopInstanceUid";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", sopInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            results.Add(new Instance(sqlDataReader));
                        }
                    }
                }
            }

            return results;
        }

        public async Task<Instance> GetInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long version)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.Instance.TableName}
                        WHERE {VLatest.Instance.StudyInstanceUid} = @studyInstanceUid
                            AND {VLatest.Instance.SeriesInstanceUid} = @seriesInstanceUid
                            AND {VLatest.Instance.SopInstanceUid} = @sopInstanceUid
                            AND {VLatest.Instance.Watermark} = @watermark";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", sopInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@watermark", version);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await sqlDataReader.ReadAsync())
                        {
                            return new Instance(sqlDataReader);
                        }

                        return null;
                    }
                }
            }
        }

        public async Task<IReadOnlyList<DeletedInstance>> GetDeletedInstanceEntriesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<DeletedInstance>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.DeletedInstance.TableName}
                        WHERE {VLatest.DeletedInstance.StudyInstanceUid} = @studyInstanceUid
                        AND {VLatest.DeletedInstance.SeriesInstanceUid} = ISNULL(@seriesInstanceUid, {VLatest.DeletedInstance.SeriesInstanceUid})
                        AND {VLatest.DeletedInstance.SopInstanceUid} = ISNULL(@sopInstanceUid, {VLatest.DeletedInstance.SopInstanceUid})
                        ORDER BY {VLatest.DeletedInstance.Watermark}";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", string.IsNullOrEmpty(seriesInstanceUid) ? DBNull.Value : seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", string.IsNullOrEmpty(sopInstanceUid) ? DBNull.Value : sopInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new DeletedInstance(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task<IReadOnlyList<ChangeFeedRow>> GetChangeFeedRowsAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                await sqlConnection.OpenAsync();

                var result = new List<ChangeFeedRow>();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @$"
                        SELECT *
                        FROM {VLatest.ChangeFeed.TableName}
                        WHERE {VLatest.ChangeFeed.StudyInstanceUid} = @studyInstanceUid
                        AND {VLatest.ChangeFeed.SeriesInstanceUid} = @seriesInstanceUid
                        AND {VLatest.ChangeFeed.SopInstanceUid} = @sopInstanceUid
                        ORDER BY {VLatest.ChangeFeed.Sequence}";

                    sqlCommand.Parameters.AddWithValue("@studyInstanceUid", studyInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@seriesInstanceUid", seriesInstanceUid);
                    sqlCommand.Parameters.AddWithValue("@sopInstanceUid", sopInstanceUid);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await sqlDataReader.ReadAsync())
                        {
                            result.Add(new ChangeFeedRow(sqlDataReader));
                        }
                    }
                }

                return result;
            }
        }

        public async Task ClearDeletedInstanceTableAsync()
        {
            await SqlTestUtils.ClearTableAsync(_connectionString, VLatest.DeletedInstance.TableName);
        }

        public async Task ClearIndexTablesAsync()
        {
            await SqlTestUtils.ClearTableAsync(_connectionString, VLatest.Instance.TableName);
            await SqlTestUtils.ClearTableAsync(_connectionString, VLatest.Series.TableName);
            await SqlTestUtils.ClearTableAsync(_connectionString, VLatest.Study.TableName);
        }
    }
}
