﻿namespace FrontierSharp.DummyModules {
    using System;
    using Interfaces;

    using OpenTK;
    using OpenTK.Graphics;
    using Util;

    class DummyEnvironmentImpl : IEnvironment {
        public EnvironmentData GetCurrent() {
            return new EnvironmentData {
                color = new ColorTypeIndexedArray<Color4> {
                    [ColorType.Horizon] = Color4.White,
                    [ColorType.Sky] = Color4.Blue,
                    [ColorType.Fog] = Color4.Gray,
                    [ColorType.Light] = Color4.Yellow,
                    [ColorType.Ambient] = Color4.Red
                },
                CloudCover = 0,
                DrawSun = true,
                Fog = new Range<float>(1, 2),
                Light = Vector3.UnitZ,
                StarFade = 0.5f,
                SunriseFade = 0.5f,
                SunsetFade = 0.5f,
                SunAngle = 45
            };
        }

        public void Init() {
            // Do nothing
        }
    }
}
