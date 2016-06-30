﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Copied from NuGetGallery (commit:f2fc834d 26.05.2016), removed V1 support and modified downloadlink

using System;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.Routing;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Atom;
using System.Collections.Generic;
using Microsoft.Data.Edm;
using NuGet.Server.Core.DataServices;
using System.Linq;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;

namespace NuGet.Server.V2.OData.Serializers
{
    public class NuGetEntityTypeSerializer
        : ODataEntityTypeSerializer
    {
        private readonly string _contentType;

        public NuGetEntityTypeSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
            _contentType = "application/zip";
        }

        public override ODataEntry CreateEntry(SelectExpandNode selectExpandNode, EntityInstanceContext entityInstanceContext)
        {
            var entry = base.CreateEntry(selectExpandNode, entityInstanceContext);

            TryAnnotateV2FeedPackage(entry, entityInstanceContext);

            return entry;
        }

        private void TryAnnotateV2FeedPackage(ODataEntry entry, EntityInstanceContext entityInstanceContext)
        {
            var instance = entityInstanceContext.EntityInstance as ODataPackage;
            if (instance != null)
            {
                // Set Atom entry metadata
                var atomEntryMetadata = new AtomEntryMetadata();
                atomEntryMetadata.Title = instance.Id;
                if (!string.IsNullOrEmpty(instance.Authors))
                {
                    atomEntryMetadata.Authors = new[] { new AtomPersonMetadata { Name = instance.Authors } };
                }
                if (instance.LastUpdated > DateTime.MinValue)
                {
                    atomEntryMetadata.Updated = instance.LastUpdated;
                }
                if (!string.IsNullOrEmpty(instance.Summary))
                {
                    atomEntryMetadata.Summary = instance.Summary;
                }
                entry.SetAnnotation(atomEntryMetadata);

                // Add package download link
                entry.MediaResource = new ODataStreamReferenceValue
                {
                    ContentType = ContentType,
                    ReadLink = BuildLinkForStreamProperty(instance, entityInstanceContext)
                };
            }
        }

        public string ContentType
        {
            get { return _contentType; }
        }

        private  Uri BuildLinkForStreamProperty(ODataPackage package, EntityInstanceContext context)
        {
            var keyValue = "Id='" + package.Id + "',Version='" + package.Version + "'";
            var downloadUrl = context.Url.CreateODataLink(new EntitySetPathSegment("Packages"), new KeyValuePathSegment(keyValue), new ActionPathSegment("Download"));
            return new Uri(downloadUrl);
        }

    }
}
