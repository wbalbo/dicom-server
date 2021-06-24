// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        private readonly ReindexConfiguration _reindexConfig;
        private readonly IReindexStateStore _reindexStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IInstanceStore _instanceStore;
        private readonly ReindexDurableFunction _reindexDurableFunction;
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly SchemaInformation _schemaInformation;

        public ReindexDurableFunctionTests()
        {
            _reindexConfig = new ReindexConfiguration();
            _reindexStore = Substitute.For<IReindexStateStore>();
            _instanceReindexer = Substitute.For<IInstanceReindexer>();
            _instanceStore = Substitute.For<IInstanceStore>();
            _schemaInformation = new SchemaInformation(1, 4);
            _schemaVersionResolver = Substitute.For<ISchemaVersionResolver>();
            _reindexDurableFunction = new ReindexDurableFunction(
                Options.Create(new IndexingConfiguration() { Add = _reindexConfig }),
                _reindexStore,
                _schemaVersionResolver,
                _schemaInformation,
                _instanceStore,
                _instanceReindexer);
        }
    }
}
