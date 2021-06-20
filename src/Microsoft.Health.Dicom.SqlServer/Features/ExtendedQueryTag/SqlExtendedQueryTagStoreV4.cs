// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    // TODO: don't need such do-nothing class
    internal class SqlExtendedQueryTagStoreV4 : SqlExtendedQueryTagStoreV3
    {

        public SqlExtendedQueryTagStoreV4(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV4> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        public override SchemaVersion Version => SchemaVersion.V4;
    }
}
