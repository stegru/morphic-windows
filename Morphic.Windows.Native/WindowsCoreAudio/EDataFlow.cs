﻿//
// EDataFlow.cs
// Morphic support library for Windows
//
// Copyright © 2020 Raising the Floor -- US Inc. All rights reserved.
//
// The R&D leading to these results received funding from the
// Department of Education - Grant H421A150005 (GPII-APCP). However,
// these results do not necessarily represent the policy of the
// Department of Education, and you should not assume endorsement by the
// Federal Government.

using System;

namespace Morphic.Windows.Native.WindowsCoreAudio
{
    // https://docs.microsoft.com/en-us/windows/win32/api/mmdeviceapi/ne-mmdeviceapi-edataflow
    internal enum EDataFlow : Int32
    {
        eRender = 0,
        eCapture = eRender + 1,
        eAll = eCapture + 1 /*,
        EDataFlow_enum_count = eAll + 1 */
    }
}
