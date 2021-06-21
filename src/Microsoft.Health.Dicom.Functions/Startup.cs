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
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("Before Config");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();

            IConfiguration configuration = builder.GetContext().Configuration?.GetSection(AzureFunctionsJobHostSection);

            var server = builder.Services.AddDicomFunctions(configuration);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("After AddDicomFunctions");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();

            server.AddSqlServer(configuration, initializeSchema: false);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("After AddSqlServer");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();

            server.AddMetadataStorageDataStoreForAzureFunction(configuration);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("After AddMetadataStorageDataStoreForAzureFunction");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            foreach (var item in builder.Services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
            System.Console.WriteLine();
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

        }
    }
}
