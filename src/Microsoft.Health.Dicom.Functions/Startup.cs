// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom.Serialization;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            IConfiguration config = builder.GetContext().Configuration.GetSection(AzureFunctionsJobHost.SectionName);

            new ServiceModule(new Core.Configs.FeatureConfiguration() { EnableExtendedQueryTags = true })
                .Load(builder.Services);

            // TODO: JsonSerializer should be customized
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());
            builder.Services.AddSingleton(jsonSerializer);

            builder.Services.AddSingleton(new RecyclableMemoryStreamManager());

            builder.Services.AddSingleton(new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max));

            builder.Services
                .AddOptions<IndexingConfiguration>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(AzureFunctionsJobHost.SectionName)
                    .GetSection(IndexingConfiguration.SectionName)
                    .Bind(sectionObj));

            builder.Services
                .AddSqlServer(config)
                .AddReindexStateStore()
                .AddIndexDataStores()
                .AddQueryStore()
                .AddInstanceStore()
                .AddChangeFeedStore()
                .AddExtendedQueryTagStores()
                .AddForegroundSchemaVersionResolution();


            builder.Services
                .AddAzureBlobServiceClient(config)
                .AddMetadataStore();

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));
        }
    }
}
