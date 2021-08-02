// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Level of a query tag.
    /// </summary>
    // TODO: make it structure . there should be constant for no version.
    public class ExtendedQueryTagVersion
    {
        private readonly byte[] _version;
        public ExtendedQueryTagVersion(byte[] version) => _version = EnsureArg.IsNotNull(version, nameof(version));

        public static ExtendedQueryTagVersion GetVersion(IReadOnlyCollection<ExtendedQueryTagVersion> queryTagVersions)
        {
            EnsureArg.IsNotNull(queryTagVersions);
            return null;
        }

        public static ExtendedQueryTagVersion GetVersion(IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags);
            return null;
        }

        public byte[] GetVersion()
        {
            return _version;
        }
    }
}
