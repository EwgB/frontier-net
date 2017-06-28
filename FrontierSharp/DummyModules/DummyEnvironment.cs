namespace FrontierSharp.DummyModules {
    using OpenTK;
    using OpenTK.Graphics;

    using Interfaces.Environment;
    using Interfaces.Property;

    using Util;

    class DummyEnvironment : IEnvironment {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public EnvironmentData Current { get {
                return new EnvironmentData {
                    Color = new ColorTypeIndexedArray<Color4> {
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
        }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }
    }
}
