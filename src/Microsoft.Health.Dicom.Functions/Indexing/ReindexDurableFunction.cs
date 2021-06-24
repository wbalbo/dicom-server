// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        private readonly ReindexConfiguration _reindexConfig;
        private readonly IReindexStateStore _reindexStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly IInstanceStore _instanceStore;
        private readonly SchemaInformation _schemaInformation;

        public ReindexDurableFunction(
            IOptions<IndexingConfiguration> configOptions,
            IReindexStateStore reindexStore,
            ISchemaVersionResolver schemaVersionResolver,
            SchemaInformation schemaInformation,
            IInstanceStore instanceStore,
            IInstanceReindexer instanceReindexer)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            EnsureArg.IsNotNull(reindexStore, nameof(reindexStore));
            EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            _reindexConfig = configOptions.Value.Add;
            _reindexStore = reindexStore;
            _instanceReindexer = instanceReindexer;
            _schemaVersionResolver = schemaVersionResolver;
            _instanceStore = instanceStore;
            _schemaInformation = schemaInformation;
        }
    }
}
