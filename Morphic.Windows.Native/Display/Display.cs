﻿// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

namespace Morphic.Windows.Native.Display
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    // ReSharper disable InconsistentNaming - Uses naming from the Windows API
    // ReSharper disable IdentifierTypo - Uses naming from the Windows API
    public class Display
    {
        /// <summary>Gets the available resolutions for a display device.</summary>
        public IEnumerable<Size> GetResolutions(string? deviceName = null)
        {
            WindowsApi.DEVMODEW mode = new WindowsApi.DEVMODEW();
            mode.Init();

            uint modeNum = 0;

            while (WindowsApi.EnumDisplaySettingsEx(deviceName, modeNum++, ref mode, 0))
            {
                yield return new Size((int)mode.dmPelsWidth, (int)mode.dmPelsHeight);
            }
        }

        private static Size Resolution(Size? newSize, string? deviceName = null)
        {
            WindowsApi.DEVMODEW mode = new WindowsApi.DEVMODEW();
            mode.Init();
            // When setting, the current display still needs to be retrieved, to pre-fill the mode struct.
            WindowsApi.EnumDisplaySettingsEx(deviceName, WindowsApi.ENUM_CURRENT_SETTINGS, ref mode, 0);
            Size originalSize = new Size((int)mode.dmPelsWidth, (int)mode.dmPelsHeight);

            if (newSize.HasValue)
            {
                const uint CDS_UPDATEREGISTRY = 1;
                mode.dmPelsWidth = (uint)newSize.Value.Width;
                mode.dmPelsHeight = (uint)newSize.Value.Height;
                WindowsApi.ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
            }

            return originalSize;
        }

        /// <summary>Gets the current resolution for a display device.</summary>
        /// <returns>The current resolution.</returns>
        public Size GetResolution(string? deviceName = null)
        {
            return Resolution(null, deviceName);
        }

        /// <summary>Gets the current resolution for a display device.</summary>
        /// <returns>The previous resolution.</returns>
        public Size SetResolution(Size resolution, string? deviceName = null)
        {
            return Resolution(resolution, deviceName);
        }
    }
}
