namespace FrontierSharp.Renderer {
    using Interfaces;

    using OpenTK;
    using OpenTK.Graphics;
    using OpenTK.Graphics.OpenGL;

    using System;
    using System.Drawing;

    public class RendererImpl : IRenderer {
        // Dependencies (injected via Ninject)
        private readonly IAvatar avatar;
        private readonly IWorld world;
        private readonly IEnvironment environment;

        private Color4 currentAmbient;

        public RendererImpl(IAvatar avatar, IWorld world, IEnvironment environment) {
            this.avatar = avatar;
            this.world = world;
            this.environment = environment;
        }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Render() {
            EnvironmentData e = this.environment.GetCurrent();
            Vector3 pos = this.avatar.GetCameraPosition();
            float waterLevel = Math.Max(this.world.GetWaterLevel(new Vector2(pos.X, pos.Y)), 0);

            if (pos.Z >= waterLevel) {
                //cfog = (current_diffuse + Color4 (0.0f, 0.0f, 1.0f)) / 2;
                //GL.Fog(FogParameter.FogStart, RENDER_DISTANCE / 2);   // Fog Start Depth
                //GL.Fog(FogParameter.FogEnd, RENDER_DISTANCE);			// Fog End Depth
                GL.Fog(FogParameter.FogStart, e.fog.Min);          // Fog Start Depth
                GL.Fog(FogParameter.FogEnd, e.fog.Max);            // Fog End Depth
            } else {
                //cfog = Color4 (0.0f, 0.5f, 0.8f);
                GL.Fog(FogParameter.FogStart, 1);               // Fog Start Depth
                GL.Fog(FogParameter.FogEnd, 32);				// Fog End Depth
            }

            GL.Enable(EnableCap.Fog);
            GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
            //GL.Fog (FogParameter.FogMode, (int) FogMode.Exp);
            GL.Fog(FogParameter.FogColor, e.color[(int)ColorType.ENV_COLOR_FOG].R);
            GL.ClearColor(e.color[(int)ColorType.ENV_COLOR_FOG]);
            //GL.ClearColor (0, 0, 0, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //GL.Clear (ClearBufferMask.DepthBufferBit);

            {
                float[] light = new float[4];

                light[0] = -e.light.X;
                light[1] = -e.light.Y;
                light[2] = -e.light.Z;
                light[3] = 0.0f;

                GL.Enable(EnableCap.Light1);
                GL.Enable(EnableCap.Lighting);
                currentAmbient = Color4.Black;
                GL.Light(LightName.Light1, LightParameter.Ambient, e.color[(int) ColorType.ENV_COLOR_AMBIENT].R);
                Color4 c = e.color[(int) ColorType.ENV_COLOR_LIGHT];
                //c *= 20.0f;
                GL.Light(LightName.Light1, LightParameter.Diffuse, c.R);
                GL.Light(LightName.Light1, LightParameter.Position, light);
            }
        }
    }
}
