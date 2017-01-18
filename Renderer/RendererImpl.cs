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

        private Color4 currentAmbient = Color4.Black;
        private Color4 currentDiffuse = Color4.White;
        private Color4 currentFog = Color4.White;
        private Size viewSize;

        public RendererImpl(IAvatar avatar, IWorld world, IEnvironment environment) {
            // Set dependencies
            this.avatar = avatar;
            this.world = world;
            this.environment = environment;
        }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
            //CgInit();
        }

        public void Render() {
            EnvironmentData e = this.environment.GetCurrent();
            Vector3 pos = this.avatar.GetCameraPosition();
            float waterLevel = Math.Max(this.world.GetWaterLevel(new Vector2(pos.X, pos.Y)), 0);

            if (pos.Z >= waterLevel) {
                //cfog = (currentDiffuse + Color4.Blue) / 2;
                //GL.Fog(FogParameter.FogStart, RENDER_DISTANCE / 2);   // Fog Start Depth
                //GL.Fog(FogParameter.FogEnd, RENDER_DISTANCE);			// Fog End Depth
                GL.Fog(FogParameter.FogStart, e.fog.Min);          // Fog Start Depth
                GL.Fog(FogParameter.FogEnd, e.fog.Max);            // Fog End Depth
            } else {
                //cfog = new Color4(0.0f, 0.5f, 0.8f);
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

            GL.Viewport(Point.Empty, viewSize);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            
            //Culling and shading
            GL.ShadeModel(ShadingModel.Smooth);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            
            //Alpha blending  
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Greater, 0);

            GL.LineWidth(2.0f);
            //GL.MatrixMode (GL_MODELVIEW);

            //Move into our unique coordanate system
            GL.LoadIdentity();
            GL.Scale(1, -1, 1);
            Vector3 angle = this.avatar.GetCameraAngle();
            GL.Rotate(angle.X, Vector3.UnitX);
            GL.Rotate(angle.Y, Vector3.UnitY);
            GL.Rotate(angle.Z, Vector3.UnitZ);
            GL.Translate(-pos.X, -pos.Y, -pos.Z);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //if (CVarUtils::GetCVar<bool>("render.shaders"))
            //    CgUpdate();
            //SceneRender();
            //CgShaderSelect(VSHADER_NONE);
            //if (CVarUtils::GetCVar<bool>("render.wireframe"))
            //    SceneRenderDebug();
            //if (CVarUtils::GetCVar<bool>("show.pages"))
            //    CacheRenderDebug();
            //TextRender();
            //if (show_map)
            //    RenderTexture(WorldMap());
            //ConsoleRender();
        }
    }
}
