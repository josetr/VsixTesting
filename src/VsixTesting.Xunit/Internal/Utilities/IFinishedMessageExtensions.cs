// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal.Utilities
{
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal static class IFinishedMessageExtensions
    {
        public static RunSummary ToRunSummary(this IFinishedMessage message)
        {
            return new RunSummary
            {
                Total = message.TestsRun,
                Failed = message.TestsFailed,
                Skipped = message.TestsSkipped,
                Time = message.ExecutionTime,
            };
        }
    }
}
