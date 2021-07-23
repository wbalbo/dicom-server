// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IStoreFactory<IExtendedQueryTagErrorStore> _extendedQueryTagErrorStoreFactory;
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
        private readonly IIndexDataStoreTestHelper _testHelper;

        public ExtendedQueryTagErrorStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _extendedQueryTagErrorStoreFactory = EnsureArg.IsNotNull(fixture.ExtendedQueryTagErrorStoreFactory, nameof(fixture.ExtendedQueryTagErrorStoreFactory));
            _indexDataStoreFactory = EnsureArg.IsNotNull(fixture.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));
            _testHelper = EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
        }
        public async Task DisposeAsync()
        {
            IExtendedQueryTagErrorStore extendedQueryTagErrorStore = await _extendedQueryTagErrorStoreFactory.GetInstanceAsync();
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await CleanupTagErrorsAsync(extendedQueryTagStore, extendedQueryTagErrorStore);
        }

        private async static Task CleanupTagErrorsAsync(IExtendedQueryTagStore extendedQueryTagStore, IExtendedQueryTagErrorStore extendedQueryTagErrorStore)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(extendedQueryTagErrorStore, nameof(extendedQueryTagErrorStore));

            var tags = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
            foreach (var tag in tags)
            {
                await extendedQueryTagErrorStore.DeleteExtendedQueryTagErrorsAsync(tag.Path);
                await extendedQueryTagStore.DeleteExtendedQueryTagAsync(tag.Path, tag.VR);
            }
        }

        // error with sopinstancekey already added

        // Add error worked

        // get error worked

        // Delete error works


        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
