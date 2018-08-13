// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;

    internal class VsHive
    {
        public VsHive(VsInstallation installation, string rootSuffix = "")
        {
            Installation = installation;
            RootSuffix = rootSuffix ?? throw new ArgumentNullException(nameof(rootSuffix));
        }

        public VsInstallation Installation { get; set; }
        public Version Version => Installation.Version;
        public string Path => Installation.Path;
        public string ApplicationPath => Installation.ApplicationPath;
        public string RootSuffix { get; }
    }
}
