// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Utilities
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    internal static class EmbeddedResourceUtil
    {
        public static string ExtractResource(Assembly assembly, string resourceName)
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, resourceName);
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new ArgumentOutOfRangeException(nameof(resourceName), $"Embedded resource {resourceName} doesn't exist in assembly {assembly.GetName().FullName}.");

                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    resourceStream.CopyTo(fileStream);
            }

            return path;
        }

        public static void ApplyDateTime(string filePath, Assembly assembly, string resourceName)
        {
            var metadataName = $"{assembly.GetName().Name}.EmbeddedFileDateTime.xml";
            using (var metadataStream = assembly.GetManifestResourceStream(metadataName))
            {
                if (metadataStream == null)
                    throw new FileNotFoundException($"Embedded resource {metadataName} doesn't exist in assembly {assembly.GetName().FullName}.");
                var xml = XDocument.Load(metadataStream);
                var item = xml.Descendants("Item").First(i => i.Attribute("Name").Value == resourceName);
                File.SetCreationTime(filePath, new DateTime(long.Parse(item.Attribute("CreationTime").Value)));
                File.SetLastWriteTime(filePath, new DateTime(long.Parse(item.Attribute("LastWriteTime").Value)));
                File.SetLastAccessTime(filePath, new DateTime(long.Parse(item.Attribute("LastAccessTime").Value)));
            }
        }
    }
}