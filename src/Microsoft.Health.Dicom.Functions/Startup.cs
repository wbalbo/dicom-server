// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Operations.Functions.Registration;
using Microsoft.Health.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        private const string AzureFunctionsJobHostSection = "AzureFunctionsJobHost";
        public override void Configure(IFunctionsHostBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            IConfiguration configuration = builder.GetContext().Configuration?.GetSection(AzureFunctionsJobHostSection);
            builder.Services.AddDicomFunctions(configuration)
                .AddSqlServer(configuration, initializeSchema: false)
                .AddBlobStorageDataStore(configuration, withHealthCheck: false)
                .AddMetadataStorageDataStore(configuration, withHealthCheck: false);

            List<ServiceDescriptor> providers = new List<ServiceDescriptor>();
            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    if (item.ImplementationType?.Name == "HealthCheckPublisherHostedService" || item.ImplementationFactory?.Target?.GetType() == typeof(TypeRegistrationBuilder))
                    {
                        providers.Add(item);
                    }
                }
            }
            foreach (var item in providers)
            {
                builder.Services.Remove(item);
            }


            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            foreach (var item in providers)
            {
                builder.Services.Remove(item);
            }

            builder.Services.Add<BlobClientProvider>()
                .Singleton()
                .AsSelf();
        }
    }
}
