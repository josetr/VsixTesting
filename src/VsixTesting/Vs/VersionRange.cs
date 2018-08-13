// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Text.RegularExpressions;
    using Common;

    internal class VersionRange
    {
        public VersionRange(string version)
        {
            var ver = Parse(version);
            Init(ver.Minimum, ver.Maximum);
        }

        public VersionRange(Version minVersion, Version maxVersion)
            => Init(minVersion, maxVersion);

        public Version Minimum { get; private set; }
        public Version Maximum { get; private set; }

        public static VersionRange Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException($"Version cannot be empty.", nameof(version));

            if (ParseSingleVersion(version, out VersionRange range) || ParseRange(version, out range))
                return range;

            throw new NotSupportedException($"The version range format {version} is not supported.");
        }

        public static bool TryParse(string version, out VersionRange ver)
        {
            try
            {
                ver = Parse(version);
                return true;
            }
            catch
            {
            }

            ver = null;
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is VersionRange range
                && obj.GetType() == range.GetType()
                && Minimum == range.Minimum
                && Maximum == range.Maximum;
        }

        public override int GetHashCode()
        {
            var hashCode = 913158992;
            hashCode = (hashCode * -1521134295) + Minimum.GetHashCode();
            hashCode = (hashCode * -1521134295) + Maximum.GetHashCode();
            return hashCode;
        }

        public override string ToString()
            => $"[{Minimum.ToString()}-{Maximum.ToString()}]";

        private void Init(Version minVersion, Version maxVersion)
        {
            Minimum = minVersion;
            Maximum = maxVersion;
        }

        private static bool ParseSingleVersion(string input, out VersionRange range)
        {
            input = NormalizeMajorVersion(input);
            var singleVersionMatch = Regex.Match(input, @"^(\d+)((?:\.\d){0,3})$");
            if (!singleVersionMatch.Success)
            {
                range = null;
                return false;
            }

            var isMissingMinorVersion = string.IsNullOrEmpty(singleVersionMatch.Groups[2].Value);

            if (isMissingMinorVersion)
            {
                var majorVersion = int.Parse(singleVersionMatch.Groups[1].Value);
                range = new VersionRange(
                    minVersion: new Version(majorVersion, 0),
                    maxVersion: new Version(majorVersion, int.MaxValue));
            }
            else
            {
                var version = Version.Parse(input);
                range = new VersionRange(version, version);
            }

            return true;
        }

        private static bool ParseRange(string version, out VersionRange range)
        {
            var rangeVersionMatch = Regex.Match(version, $@"^(\(|\[)?([\d|\.]+)-([\d|\.]+)?(\)|\])?$");
            if (!rangeVersionMatch.Success)
            {
                range = null;
                return false;
            }

            var left = TextOr(rangeVersionMatch.Groups[1].Value, "[");
            var minimumVersionText = NormalizeVersion(rangeVersionMatch.Groups[2].Value, 0);
            var minimunVersion = new Version(minimumVersionText);

            var maximumVersionText = NormalizeVersion(rangeVersionMatch.Groups[3].Value, int.MaxValue);
            var maximumVersion = string.IsNullOrEmpty(maximumVersionText)
                ? new Version(int.MaxValue, int.MaxValue)
                : new Version(maximumVersionText);
            var right = TextOr(rangeVersionMatch.Groups[4].Value, "]");

            if (left == "(" /* is exclusive*/)
                minimunVersion = minimunVersion.IncreaseMinorVersion();
            if (right == ")" /* is exclusive*/)
                maximumVersion = maximumVersion.DecreaseMinorVersion();
            range = new VersionRange(minimunVersion, maximumVersion);
            return true;
        }

        private static string NormalizeVersion(string version, int minorVersion)
        {
            version = NormalizeMajorVersion(version);
            var pattern = @"^(\d+)$";
            if (Regex.IsMatch(version, pattern))
                return version + "." + minorVersion;
            return version;
        }

        private static string TextOr(string text, string or) =>
            !string.IsNullOrEmpty(text) ? text : or;

        private static string NormalizeMajorVersion(string version)
        {
            return Regex.Replace(version, @"^(\d{4})\b", a =>
            {
                var majorVersion = VersionUtil.FromYear(int.Parse(a.Groups[1].Value)).Major;
                return majorVersion.ToString();
            });
        }
    }
}