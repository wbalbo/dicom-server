// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Core.Registration
{
    public static class ServiceCollectionExtensions
    {
        public static void OutputHostedService(this IServiceCollection services, string title)
        {
            EnsureArg.IsNotNull(services);
            System.Console.WriteLine(title);
            foreach (var item in services)
            {
                if (item.ServiceType == typeof(IHostedService))
                {
                    System.Console.WriteLine(item);
                }
            }
        }
    }
}
