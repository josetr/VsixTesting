// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System;

    internal static class VersionExtensions
    {
        public static Version IncreaseMinorVersion(this Version version)
        {
            if (version.Minor < int.MaxValue)
                return NewVersion(version.Major, version.Minor + 1, version.Build, version.Revision);
            if (version.Major != int.MaxValue)
                return NewVersion(version.Major + 1, 0, version.Build, version.Revision);
            return version;
        }

        public static Version DecreaseMinorVersion(this Version version)
        {
            if (version.Minor >= 1)
                return NewVersion(version.Major, version.Minor - 1, version.Build, version.Revision);
            if (version.Major != 0)
                return NewVersion(version.Major - 1, int.MaxValue, version.Build, version.Revision);
            return version;
        }

        private static Version NewVersion(int major, int minor, int build, int revision)
        {
            if (revision >= 0)
                return new Version(major, minor, build, revision);
            if (build >= 0)
                return new Version(major, minor, build);
            return new Version(major, minor);
        }
    }
}
