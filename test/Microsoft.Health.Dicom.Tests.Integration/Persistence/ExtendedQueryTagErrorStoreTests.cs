// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IStoreFactory<IExtendedQueryTagErrorStore> _extendedQueryTagErrorStoreFactory;
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
        private readonly IIndexDataStoreTestHelper _testHelper;
        private readonly DateTime _definedNow;

        public ExtendedQueryTagErrorStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreFactory, nameof(fixture.ExtendedQueryTagStoreFactory));
            _extendedQueryTagErrorStoreFactory = EnsureArg.IsNotNull(fixture.ExtendedQueryTagErrorStoreFactory, nameof(fixture.ExtendedQueryTagErrorStoreFactory));
            _indexDataStoreFactory = EnsureArg.IsNotNull(fixture.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));
            _testHelper = EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
            _definedNow = DateTime.UtcNow;
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

        private async Task<ExtendedQueryTagStoreEntry> CreateTagInStoreAsync(IExtendedQueryTagStore extendedQueryTagStore, CancellationToken cancellationToken = default)
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();

            await extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1 }, 128, ready: true, cancellationToken: cancellationToken);

            var actualExtendedQueryTagEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync(extendedQueryTagEntry1.Path);

            Assert.Equal(actualExtendedQueryTagEntries[0].Path, extendedQueryTagEntry1.Path);
            return actualExtendedQueryTagEntries[0];
        }

        [Fact]
        public async Task GivenValidExtendedQueryTagError_WhenAddExtendedQueryTagError_ThenTagShouldBeAdded()
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            ExtendedQueryTagStoreEntry actualTagEntry = await CreateTagInStoreAsync(extendedQueryTagStore);
            // error store
            IExtendedQueryTagErrorStore extendedQueryTagErrorStore = await _extendedQueryTagErrorStoreFactory.GetInstanceAsync();
            int actualErrorCode = 3;
            string actualStudyInstanceUid = Guid.NewGuid().ToString();
            string actualSeriesInstanceUid = Guid.NewGuid().ToString();
            string actualSopInstanceUid = Guid.NewGuid().ToString();
            var actualSopInstanceKey = 123123;

            int outputTagKey = await extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key,
                _definedNow,
                actualErrorCode,
                actualStudyInstanceUid,
                actualSeriesInstanceUid,
                actualSopInstanceUid,
                actualSopInstanceKey);

            Assert.Equal(outputTagKey, actualTagEntry.Key);

            var extendedQueryTagError = await extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);

            Assert.Equal(extendedQueryTagError[0].UtcCreatedTime, _definedNow);
            Assert.Equal(extendedQueryTagError[0].StudyInstanceUid, actualStudyInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SeriesInstanceUid, actualSeriesInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SopInstanceUid, actualSopInstanceUid);
            // TODO validate error Message
        }

        [Fact]
        public async Task GivenNonExistingQueryTag_WhenAddExtendedQueryTagError_ThenShouldThrowException()
        {
            IExtendedQueryTagErrorStore extendedQueryTagErrorStore = await _extendedQueryTagErrorStoreFactory.GetInstanceAsync();
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                0, _definedNow, 0, "123", null, null, 0));
        }

        [Fact]
        public async Task GivenExistingQueryTagError_WhenAddExtendedQueryTagError_ThenShouldThrowException()
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            ExtendedQueryTagStoreEntry actualTagEntry = await CreateTagInStoreAsync(extendedQueryTagStore);

            long sopInstanceKey = 0;

            IExtendedQueryTagErrorStore extendedQueryTagErrorStore = await _extendedQueryTagErrorStoreFactory.GetInstanceAsync();
            await extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key, _definedNow, 0, "123", null, null, sopInstanceKey);

            var extendedQueryTagError = await extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);
            Assert.Equal(1, extendedQueryTagError.Count);

            await Assert.ThrowsAsync<ExtendedQueryTagErrorAlreadyExistsException>(() => extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key, _definedNow, 0, "123", null, null, sopInstanceKey));
        }

        [Fact]
        public async Task GivenExistingQueryTagError_WhenDeletingExtendedQueryTagErrors_ThenShouldDeleteAllErrorsForTag()
        {
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            ExtendedQueryTagStoreEntry actualTagEntry = await CreateTagInStoreAsync(extendedQueryTagStore);

            IExtendedQueryTagErrorStore extendedQueryTagErrorStore = await _extendedQueryTagErrorStoreFactory.GetInstanceAsync();
            await extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key, _definedNow, 0, "123", null, null, 0);
            await extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                actualTagEntry.Key, _definedNow, 0, "123", null, null, 1);

            var extendedQueryTagErrorBeforeDelete = await extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);
            Assert.Equal(2, extendedQueryTagErrorBeforeDelete.Count);

            await extendedQueryTagErrorStore.DeleteExtendedQueryTagErrorsAsync(actualTagEntry.Path);

            var extendedQueryTagErrorAfterDelete = await extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(actualTagEntry.Path);
            Assert.Equal(0, extendedQueryTagErrorAfterDelete.Count);

        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
