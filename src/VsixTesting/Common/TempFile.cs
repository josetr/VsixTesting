// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System;
    using System.IO;

    internal sealed class TempFile : IDisposable
    {
        public TempFile(string path)
        {
            Path = path;
        }

        ~TempFile()
        {
            TryDeleteFile();
        }

        public string Path { get; private set; }

        public void Dispose()
        {
            if (TryDeleteFile())
                GC.SuppressFinalize(this);
        }

        private bool TryDeleteFile()
        {
            if (Path == null)
            {
                try
                {
                    File.Delete(Path);
                    Path = null;
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}