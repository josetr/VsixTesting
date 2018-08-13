// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal.Utilities
{
    using System;
    using Xunit.Sdk;

    [Serializable]
    internal class SerializableRunSummary
    {
        public int Total { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public decimal Time { get; set; }

        public static implicit operator RunSummary(SerializableRunSummary summary) => new RunSummary
        {
            Total = summary.Total,
            Failed = summary.Failed,
            Skipped = summary.Skipped,
            Time = summary.Time,
        };
    }
}