// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Metadata;
using Microsoft.Health.Dicom.Metadata.Features.Health;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderMetadataRegistrationExtensions
    {
        private const string DicomServerBlobConfigurationSectionName = "DicomWeb:MetadataStore";

        /// <summary>
        /// Adds the metadata store for the DICOM server.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The server builder.</returns>
        public static IDicomServerBuilder AddMetadataStorageDataStore(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            serverBuilder.AddMetadataPersistence(configuration);
            serverBuilder.AddMetadataHealthCheck();

            return serverBuilder;
        }

        public static IDicomServerBuilder AddMetadataStorageDataStoreForAzureFunction(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            IServiceCollection services = serverBuilder.Services;

            services.AddBlobDataStoreImp();

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            services.Add(sp =>
            {
                ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();
                IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfiguration = sp.GetService<IOptionsMonitor<BlobContainerConfiguration>>();
                BlobContainerConfiguration blobContainerConfiguration = namedBlobContainerConfiguration.Get(Constants.ContainerConfigurationName);

                return new BlobContainerInitializer(
                    blobContainerConfiguration.ContainerName,
                    loggerFactory.CreateLogger<BlobContainerInitializer>());
            })
                .Singleton()
                .AsService<IBlobContainerInitializer>();

            services.AddScoped<IMetadataStore, BlobMetadataStore>();
            services.AddScoped(typeof(BlobMetadataStore));

            services.Output<BlobMetadataStore>("PENCHE BlobMetadataStore");

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<IMetadataStore, LoggingMetadataStore>();

            return serverBuilder;
        }
        public const string BlobStoreConfigurationSectionName = "BlobStore";
        private static IServiceCollection AddBlobDataStoreImp(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            if (services.Any(x => x.ImplementationType == typeof(BlobClientProvider)))
            {
                return services;
            }

            services.Add(provider =>
            {
                var config = new BlobDataStoreConfiguration();
                provider.GetService<IConfiguration>().GetSection(BlobStoreConfigurationSectionName).Bind(config);
                configureAction?.Invoke(config);

                if (string.IsNullOrEmpty(config.ConnectionString) && config.AuthenticationType == BlobDataStoreAuthenticationType.ConnectionString)
                {
                    config.ConnectionString = "UseDevelopmentStorage=true";
                }

                return config;
            })
                .Singleton()
                .AsSelf();


            services.Add<BlobClientProvider>()
                .Singleton()
                .AsSelf()
                .AsService<IRequireInitializationOnFirstRequest>(); // so that web requests block on its initialization.

            services.Add(sp =>
                {
                    var clientProvider = sp.GetService<BlobClientProvider>();
                    return clientProvider.CreateBlobClient();
                })
                .Singleton()
                .AsSelf();

            services.Add<BlobClientReadWriteTestProvider>()
                    .Singleton()
                    .AsService<IBlobClientTestProvider>();

            services.Add<MyBlobClientInitializer>()
                .Singleton()
                .AsService<IBlobClientInitializer>();

            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            return services;
        }

        private static IDicomServerBuilder AddMetadataPersistence(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            IServiceCollection services = serverBuilder.Services;

            services.AddBlobDataStore();

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            services.Add(sp =>
                {
                    ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();
                    IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfiguration = sp.GetService<IOptionsMonitor<BlobContainerConfiguration>>();
                    BlobContainerConfiguration blobContainerConfiguration = namedBlobContainerConfiguration.Get(Constants.ContainerConfigurationName);

                    return new BlobContainerInitializer(
                        blobContainerConfiguration.ContainerName,
                        loggerFactory.CreateLogger<BlobContainerInitializer>());
                })
                .Singleton()
                .AsService<IBlobContainerInitializer>();

            services.Add<BlobMetadataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<IMetadataStore, LoggingMetadataStore>();

            return serverBuilder;
        }

        private static IDicomServerBuilder AddMetadataHealthCheck(this IDicomServerBuilder serverBuilder)
        {
            serverBuilder.Services.AddHealthChecks().AddCheck<MetadataHealthCheck>(name: "MetadataHealthCheck");
            return serverBuilder;
        }
    }
}
