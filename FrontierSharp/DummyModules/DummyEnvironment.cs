namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Environment;
    using Common.Property;
    using Common.Util;

    class DummyEnvironment : IEnvironment {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public EnvironmentData Current { get {
                return new EnvironmentData {
                    Color = new ColorTypeArray {
                        [ColorTypes.Horizon] = Color3.White,
                        [ColorTypes.Sky] = Color3.Blue,
                        [ColorTypes.Fog] = Color3.Gray,
                        [ColorTypes.Light] = Color3.Yellow,
                        [ColorTypes.Ambient] = Color3.Red
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
