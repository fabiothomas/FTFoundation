using System;

namespace FTFoundation.Core
{
    [Flags]
    public enum Platform
    {
        Editor = 0,
        Standalone = 1,
        Desktop = Standalone,
        IOS = 2,
        Android = 4,
        WebGL = 8,
        WSA = 16,
        PS4 = 32,
        XboxOne = 64,
        TvOS = 128,
        Switch = 256,
        LinuxHeadlessSimulation = 512,
        PS5 = 1024,
        VisionOS = 2048
    }
}