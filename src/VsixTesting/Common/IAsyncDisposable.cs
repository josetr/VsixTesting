// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System.Threading.Tasks;

    internal interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}