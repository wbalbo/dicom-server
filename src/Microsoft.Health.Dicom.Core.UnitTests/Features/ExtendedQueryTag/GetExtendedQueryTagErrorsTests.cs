// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsTests
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IGetExtendedQueryTagErrorsService _getExtendedQueryTagsService;

        public GetExtendedQueryTagErrorsTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            var factory = Substitute.For<IStoreFactory<IExtendedQueryTagStore>>();
            factory.GetInstanceAsync(default).Returns(_extendedQueryTagStore);
            _getExtendedQueryTagsService = new GetExtendedQueryTagErrorsService(factory, _dicomTagParser);
        }

        // no tags
        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagDoesNotExist_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            // valid tag
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagErrorsAsync(tagPath);

            Assert.Empty(response.ExtendedQueryTagErrors);
        }

        // tag with no error

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagHasNoError_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            // valid tag
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagErrorsAsync(tagPath);

            // CHECK FOR RESPONSE MESSAGE?
        }


        // tag with error

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagExists_ThenReturnExtendedQueryTagErrorsList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            var expected = new List<ExtendedQueryTagError> { CreateExtendedQueryTagError("hello123", Guid.NewGuid().ToString(), DateTime.UtcNow) };

            //expected.Select(x => new ExtendedQueryTagError() )
            // valid tag
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(expected);
            GetExtendedQueryTagErrorsResponse response = await _getExtendedQueryTagsService.GetExtendedQueryTagErrorsAsync(tagPath);

            Assert.Equal(expected, response.ExtendedQueryTagErrors);
        }

        private static ExtendedQueryTagError CreateExtendedQueryTagError(
            string errorMessage,
            string studyInstanceUid,
            DateTime timestamp,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            return new ExtendedQueryTagError(timestamp, studyInstanceUid, seriesInstanceUid, sopInstanceUid, errorMessage);
        }

        // ?pagination

        //
    }
}
