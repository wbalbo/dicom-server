﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represent each extended query tag entry has retrieved from the store.
    /// </summary>
    public class ExtendedQueryTagStoreEntry : ExtendedQueryTagEntry
    {
        // TODO: why all properties has Set?
        public ExtendedQueryTagStoreEntry(int key, string path, string vr, string privateCreator, QueryTagLevel level, ExtendedQueryTagStatus status, ExtendedQueryTagVersion version)
        {

            Key = key;
            Path = EnsureArg.IsNotNullOrWhiteSpace(path, nameof(path));
            VR = EnsureArg.IsNotNullOrWhiteSpace(vr, nameof(vr));
            PrivateCreator = privateCreator;
            Level = level;
            Status = status;
            Version = EnsureArg.IsNotNull(version, nameof(version));
        }

        /// <summary>
        /// Key of this extended query tag entry.
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Status of this tag.
        /// </summary>
        public ExtendedQueryTagStatus Status { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public QueryTagLevel Level { get; set; }

        public ExtendedQueryTagVersion Version { get; set; }

        /// <summary>
        /// Convert to  <see cref="GetExtendedQueryTagEntry"/>.
        /// </summary>
        /// <returns>The extended query tag entry.</returns>
        public GetExtendedQueryTagEntry ToExtendedQueryTagEntry()
        {
            return new GetExtendedQueryTagEntry { Path = Path, VR = VR, PrivateCreator = PrivateCreator, Level = Level, Status = Status };
        }

        public override string ToString()
        {
            return $"Key: {Key}, Path: {Path}, VR:{VR}, PrivateCreator:{PrivateCreator}, Level:{Level}, Status:{Status}";
        }
    }
}
