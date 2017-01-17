namespace FrontierSharp.DummyModules {
    using Interfaces;

    using OpenTK;
    using OpenTK.Graphics;

    class DummyEnvironmentImpl : IEnvironment {
        public EnvironmentData GetCurrent() {
            return new EnvironmentData {
                color = new Color4[] {
                    Color4.White,
                    Color4.Blue,
                    Color4.Gray,
                    Color4.Yellow,
                    Color4.Red
                },
                cloud_cover = 0,
                draw_sun = true,
                fog = new Range<float>(1, 2),
                light = Vector3.UnitZ,
                star_fade = 0.5f,
                sunrise_fade = 0.5f,
                sunset_fade = 0.5f,
                sun_angle = 45
            };
        }
    }
}
