// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Dicom;
//using EnsureThat;
//using Microsoft.Health.Dicom.Core.Exceptions;
//using Microsoft.Health.Dicom.Core.Extensions;
//using Microsoft.Health.Dicom.Core.Features.Common;
//using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
//using Microsoft.Health.Dicom.Core.Features.Store;
//using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
//using Microsoft.Health.Dicom.Tests.Common;
//using Microsoft.Health.Dicom.Tests.Common.Extensions;
//using Xunit;

//namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
//{
//    public class ExtendedQueryTagErrorStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
//    {
//        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
//        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
//        private readonly IIndexDataStoreTestHelper _testHelper;

//        public ExtendedQueryTagErrorStoreTests(SqlDataStoreTestsFixture fixture)
//        {
//            EnsureArg.IsNotNull(fixture, nameof(fixture));
//            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreFactory, nameof(fixture.ExtendedQueryTagStoreFactory));
//            _indexDataStoreFactory = EnsureArg.IsNotNull(fixture.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));
//            _testHelper = EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
//        }
//        public async Task DisposeAsync()
//        {
//            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
//            await CleanupTagErrorsAsync(extendedQueryTagStore);
//        }

//        private async Task CleanupTagErrorsAsync(IExtendedQueryTagStore extendedQueryTagStore)
//        {
//            //var tags = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
//            //foreach (var tag in tags)
//            //{
//            //    await extendedQueryTagStore.DeleteExtendedQueryTagAsync(tag.Path, tag.VR);
//            //}
//            // FIND A WAY TO delete everything created
//        }

//        public Task InitializeAsync()
//        {
//            throw new System.NotImplementedException();
//        }
//    }
//}
