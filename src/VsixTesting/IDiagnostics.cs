// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace VsixTesting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IDiagnostics : IOutput
    {
        Task<T> RunAsync<T>(string displayName, Func<IOutput, Task<T>> function);
    }

    internal interface IOutput
    {
        void WriteLine(string message);
        void WriteLine(string format, params object[] args);
    }

    internal static class IDiagnosticsExtensions
    {
        public static Task<T> RunAsync<T>(this IDiagnostics diagnostics, string displayName, Func<Task<T>> function)
            => diagnostics.RunAsync(displayName, output => function());

        public static Task RunAsync(this IDiagnostics diagnostics, string displayName, Func<IOutput, Task> function)
            => diagnostics.RunAsync(displayName, async output =>
            {
                await function(output);
                return 0;
            });

        public static Task RunAsync(this IDiagnostics diagnostics, string displayName, Action<IOutput> function)
            => diagnostics.RunAsync(displayName, output =>
            {
                function(output);
                return Task.FromResult(0);
            });

        public static Task RunAsync(this IDiagnostics diagnostics, string displayName, Func<Task> function)
            => diagnostics.RunAsync(displayName, output => function());

        public static Task RunAsync(this IDiagnostics diagnostics, string displayName, Action action)
        {
            return diagnostics.RunAsync(displayName, output =>
            {
                action();
                return Task.FromResult(0);
            });
        }
    }

    internal class DiagnosticOutput : IOutput
    {
        private int id = 0;

        public ConcurrentBag<(DateTime date, int id, string message)> Entries { get; set; }
            = new ConcurrentBag<(DateTime date, int id, string message)>();

        public void WriteLine(string message)
            => Entries.Add((DateTime.Now, Interlocked.Increment(ref id), message));

        public void WriteLine(string format, params object[] args)
            => WriteLine(string.Format(format, args));

        public override string ToString()
            => Entries
            .OrderBy(msg => (msg.date, msg.id))
            .Aggregate(new StringBuilder(), (sb, msg) => sb.AppendLine(msg.message), sb => sb.ToString());
    }

    internal class MultiOutput : IOutput
    {
        private readonly List<IOutput> outputs = new List<IOutput>();

        public MultiOutput(IEnumerable<IOutput> outputs)
            => this.outputs.AddRange(outputs);

        public void WriteLine(string message)
            => outputs.ForEach(output => output.WriteLine(message));

        public void WriteLine(string format, params object[] args)
            => outputs.ForEach(output => output.WriteLine(format, args));
    }
}