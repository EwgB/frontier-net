namespace FrontierSharp.Common.Region {
    using System;
    using System.Diagnostics.CodeAnalysis;

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegionFlag {
        Test = 0x0001,
        Mesas = 0x0002,
        Crater = 0x0004,
        Beach = 0x0008,
        BeachCliff = 0x0010,
        Sinkhole = 0x0020,
        Crack = 0x0040,
        Tiered = 0x0080,
        CanyonNS = 0x0100,
        NoBlend = 0x0200,

        RiverN = 0x1000,
        RiverE = 0x2000,
        RiverS = 0x4000,
        RiverW = 0x8000,

        RiverNS = (RiverN | RiverS),
        RiverEW = (RiverE | RiverW),
        RiverNW = (RiverN | RiverW),
        RiverSE = (RiverS | RiverE),
        RiverNE = (RiverN | RiverE),
        RiverSW = (RiverS | RiverW),
        RiverAny = (RiverNS | RiverEW)
    }
}
