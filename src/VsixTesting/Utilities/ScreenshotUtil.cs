// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Utilities
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using static Interop.User32;

    internal static class ScreenshotUtil
    {
        public static void CaptureWindow(IntPtr hwnd, string path)
        {
            var rect = default(RECT);
            GetWindowRect(hwnd, ref rect);
            CaptureScreenArea(
                path,
                left: rect.Left,
                top: rect.Top,
                width: rect.Right - rect.Left,
                height: rect.Bottom - rect.Top);
        }

        public static void CaptureScreenArea(string path, int left, int top, int width, int height)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var image = Graphics.FromImage(bitmap))
            {
                image.CopyFromScreen(
                    sourceX: left,
                    sourceY: top,
                    blockRegionSize: new Size(width, height),
                    copyPixelOperation: CopyPixelOperation.SourceCopy,
                    destinationX: 0,
                    destinationY: 0);

                bitmap.Save(path, ImageFormat.Png);
            }
        }
    }
}
