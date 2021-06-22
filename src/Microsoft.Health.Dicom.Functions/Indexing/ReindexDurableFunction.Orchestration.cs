// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        ///  The orchestration function to add extended query tags.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(ReindexTagsAsync))]
        public async Task ReindexTagsAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            logger = context.CreateReplaySafeLogger(logger);
            var tagKeys = context.GetInput<IReadOnlyList<int>>();
            ReindexOperation reindexOperation = await context.CallActivityAsync<ReindexOperation>(
                nameof(PrepareReindexingTagsAsync),
                new PrepareReindexingTagsInput { OperationId = context.InstanceId, TagKeys = tagKeys });

            var storeEntries =
                await context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(GetProcessingTagsAsync), reindexOperation.OperationId);
            ReindexInstanceInput reindexInstanceInput = new ReindexInstanceInput
            {
                TagStoreEntries = storeEntries,
                WatermarkRange = (Start: reindexOperation.StartWatermark, End: reindexOperation.EndWatermark)
            };
            await context.CallActivityAsync(nameof(ReindexInstancesAsync), reindexInstanceInput);

            await context.CallActivityAsync(nameof(CompleteReindexingTagsAsync), reindexOperation.OperationId);
        }
    }
}
