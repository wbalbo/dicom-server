// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Durable;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        private readonly IGuidFactory _guidFactory;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IInstanceStore _instanceStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly QueryTagIndexingOptions _options;

        public ReindexDurableFunction(
            IGuidFactory guidFactory,
            IExtendedQueryTagStore extendedQueryTagStore,
            IInstanceStore instanceStore,
            IInstanceReindexer instanceReindexer,
            ISchemaVersionResolver schemaVersionResolver,
            IOptions<JsonSerializerOptions> jsonOptions,
            IOptions<QueryTagIndexingOptions> configOptions)
        {
            _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            _instanceReindexer = EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            _schemaVersionResolver = EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            _jsonOptions = EnsureArg.IsNotNull(jsonOptions?.Value, nameof(jsonOptions));
            _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
        }
    }
}
