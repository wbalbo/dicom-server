﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public interface IUpdateExtendedQueryTagService
    {
        /// <summary>
        /// Update extended query tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The return tag entry.</returns>
        public Task<GetExtendedQueryTagEntry> UpdateExtendedQueryTagAsync(string tagPath, UpdateExtendedQueryTagEntry newValue, CancellationToken cancellationToken = default);
    }
}
