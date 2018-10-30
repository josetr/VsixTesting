// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;

    internal static class VersionUtil
    {
        public static int GetYear(Version version)
        {
            switch (version.Major)
            {
                case 11: return 2012;
                case 12: return 2013;
                case 14: return 2015;
                case 15: return 2017;
            }

            throw new ArgumentOutOfRangeException(nameof(version), $"Visual Studio {version.Major} is not supported.");
        }

        public static Version FromYear(int year)
        {
            switch (year)
            {
                case 2012: return new Version(11, 0);
                case 2013: return new Version(12, 0);
                case 2015: return new Version(14, 0);
                case 2017: return new Version(15, 0);
            }

            throw new ArgumentOutOfRangeException(nameof(year), $"Visual Studio {year} is not supported.");
        }
    }
}
