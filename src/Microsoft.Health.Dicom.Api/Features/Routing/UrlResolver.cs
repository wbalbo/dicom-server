﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    public sealed class UrlResolver : IUrlResolver
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;

        public UrlResolver(
            IUrlHelperFactory urlHelperFactory,
            IHttpContextAccessor httpContextAccessor,
            IActionContextAccessor actionContextAccessor)
        {
            EnsureArg.IsNotNull(urlHelperFactory, nameof(urlHelperFactory));
            EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
            EnsureArg.IsNotNull(actionContextAccessor, nameof(actionContextAccessor));

            _urlHelperFactory = urlHelperFactory;
            _httpContextAccessor = httpContextAccessor;
            _actionContextAccessor = actionContextAccessor;
        }

        private IUrlHelper UrlHelper => _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

        /// <inheritdoc />
        public Uri ResolveOperationStatusUri(Guid operationId)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedOperationStatus : KnownRouteNames.OperationStatus,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.OperationId, OperationId.ToString(operationId) },
                });
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagUri(string tagPath)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedGetExtendedQueryTag : KnownRouteNames.GetExtendedQueryTag,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.TagPath, tagPath },
                });
        }

        /// <inheritdoc />
        public Uri ResolveQueryTagErrorsUri(string tagPath)
        {
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedGetExtendedQueryTagErrors : KnownRouteNames.GetExtendedQueryTagErrors,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.TagPath, tagPath },
                });
        }

        /// <inheritdoc />
        public Uri ResolveRetrieveStudyUri(string studyInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedRetrieveStudy : KnownRouteNames.RetrieveStudy,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.StudyInstanceUid, studyInstanceUid },
                });
        }

        /// <inheritdoc />
        public Uri ResolveRetrieveInstanceUri(InstanceIdentifier instanceIdentifier)
        {
            EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
            var hasVersion = _httpContextAccessor.HttpContext.Request.RouteValues.ContainsKey("version");

            return RouteUri(
                hasVersion ? KnownRouteNames.VersionedRetrieveInstance : KnownRouteNames.RetrieveInstance,
                new RouteValueDictionary
                {
                    { KnownActionParameterNames.StudyInstanceUid, instanceIdentifier.StudyInstanceUid },
                    { KnownActionParameterNames.SeriesInstanceUid, instanceIdentifier.SeriesInstanceUid },
                    { KnownActionParameterNames.SopInstanceUid, instanceIdentifier.SopInstanceUid },
                });
        }

        private Uri RouteUri(string routeName, RouteValueDictionary routeValues)
        {
            HttpRequest request = _httpContextAccessor.HttpContext.Request;

            return new Uri(
                UrlHelper.RouteUrl(
                    routeName,
                    routeValues,
                    request.Scheme,
                    request.Host.Value));
        }
    }
}
