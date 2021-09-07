﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Durable;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing.Models
{
    public class ReindexInputTests
    {
        [Fact]
        public void GivenEmptyInput_WhenGettingProgress_ThenReturnDefaultStatus()
        {
            OperationProgress progress = new ReindexInput().GetProgress();
            Assert.Equal(0, progress.PercentComplete);
            Assert.Null(progress.ResourceIds);
        }

        [Fact]
        public void GivenMinimumCompletion_WhenGettingProgress_ThenReturnCompletedProgress()
        {
            OperationProgress progress = new ReindexInput { Completed = new WatermarkRange(1, 1) }.GetProgress();
            Assert.Equal(100, progress.PercentComplete);
            Assert.Null(progress.ResourceIds);
        }

        [Theory]
        [InlineData(4, 4, 25, "01010101,02020202")]
        [InlineData(3, 4, 50, "01010101,02020202")]
        [InlineData(2, 4, 75, "01010101,02020202")]
        [InlineData(1, 4, 100, "01010101,02020202,03030303")]
        public void GivenReindexInput_WhenGettingProgress_ThenReturnComputedProgress(int start, int end, int expected, string resourceIds)
        {
            string[] tags = resourceIds.Split(',');

            OperationProgress progress = new ReindexInput
            {
                Completed = new WatermarkRange(start, end),
                QueryTags = tags.Select((x, i) => new ExtendedQueryTagReference { Key = i, Path = x }).ToList(),
            }.GetProgress();

            Assert.Equal(expected, progress.PercentComplete);
            Assert.True(progress.ResourceIds.SequenceEqual(tags));
        }
    }
}
