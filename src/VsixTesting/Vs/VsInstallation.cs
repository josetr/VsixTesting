// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class VsInstallation
    {
        public VsInstallation(Version version, string path, string name)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public Version Version { get; }
        public bool Preview => Name.Contains("-pre");
        public string Path { get; }
        public string Name { get; }
        public string ProductId { get; set; } = string.Empty;
        public string ApplicationPath => VisualStudioUtil.GetApplicationPath(Path);
        public IEnumerable<string> PackageIds { get; set; } = Enumerable.Empty<string>();
    }
}
