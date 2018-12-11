namespace FrontierSharp.Renderer {
    using System;
    using System.Drawing;

    using Ninject;
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Avatar;
    using Common.Environment;
    using Common.Property;
    using Common.Renderer;
    using Common.Scene;
    using Common.Shaders;
    using Common.Util;
    using Common.World;

    internal class RendererImpl : IRenderer {
        #region Constants

        private const int MAP_SIZE = 512;

        #endregion

        #region Modules

        private readonly IKernel kernel;

        private IAvatar avatar;
        private IAvatar Avatar => avatar ?? (avatar = kernel.Get<IAvatar>());

        private ICache cache;
        private ICache Cache => cache ?? (cache = kernel.Get<ICache>());

        private IConsole console;
        private IConsole Console => console ?? (console = kernel.Get<IConsole>());

        private IEnvironment environment;
        private IEnvironment Environment => environment ?? (environment = kernel.Get<IEnvironment>());

        private IScene scene;
        private IScene Scene => scene ?? (scene = kernel.Get<IScene>());

        private IShaders shaders;
        private IShaders Shaders => shaders ?? (shaders = kernel.Get<IShaders>());

        private IText text;
        private IText Text => text ?? (text = kernel.Get<IText>());

        private IWorld world;
        private IWorld World => world ?? (world = kernel.Get<IWorld>());

        #endregion

        #region Public properties

        public IProperties Properties => RendererProperties;
        public IRendererProperties RendererProperties { get; } = new RendererProperties();

        #endregion

        #region Private properties

        private bool showMap;
        private int viewWidth;
        private int viewHeight;

        #endregion

        public RendererImpl(IKernel kernel) {
            this.kernel = kernel;
        }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Render() {
            if (renderLoadingScreen) {
                RenderLoadingScreen();
                renderLoadingScreen = false;
            }
            else {

                var envData = Environment.Current;
                var pos = Avatar.CameraPosition;
                var waterLevel = Math.Max(World.GetWaterLevel(new Vector2(pos.X, pos.Y)), 0);

                if (pos.Z >= waterLevel) {
                    //currentFog = (currentDiffuse + Color3.Blue) / 2;
                    //GL.Fog(FogParameter.FogStart, RENDER_DISTANCE / 2);   // Fog Start Depth
                    //GL.Fog(FogParameter.FogEnd, RENDER_DISTANCE);			// Fog End Depth
                    GL.Fog(FogParameter.FogStart, envData.Fog.Min); // Fog Start Depth
                    GL.Fog(FogParameter.FogEnd, envData.Fog.Max); // Fog End Depth
                }
                else {
                    //cfog = new Color3(0.0f, 0.5f, 0.8f);
                    GL.Fog(FogParameter.FogStart, 1); // Fog Start Depth
                    GL.Fog(FogParameter.FogEnd, 32); // Fog End Depth
                }

                GL.Enable(EnableCap.Fog);
                GL.Fog(FogParameter.FogMode, (int) FogMode.Linear);
                //GL.Fog (FogParameter.FogMode, (int) FogMode.Exp);
                GL.Fog(FogParameter.FogColor, envData.Color[ColorTypes.Fog].R);
                GL.ClearColor((Color) envData.Color[ColorTypes.Fog]);
                //GL.ClearColor (0, 0, 0, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                //GL.Clear (ClearBufferMask.DepthBufferBit);

                var light = new[] {
                    -envData.Light.X,
                    -envData.Light.Y,
                    -envData.Light.Z,
                    0
                };

                GL.Enable(EnableCap.Light1);
                GL.Enable(EnableCap.Lighting);
                GL.Light(LightName.Light1, LightParameter.Ambient, envData.Color[ColorTypes.Ambient].R);
                var c = envData.Color[ColorTypes.Light];
                //c *= 20.0f;
                GL.Light(LightName.Light1, LightParameter.Diffuse, c.R);
                GL.Light(LightName.Light1, LightParameter.Position, light);

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
                var angle = Avatar.CameraAngle;
                GL.Rotate(angle.X, Vector3.UnitX);
                GL.Rotate(angle.Y, Vector3.UnitY);
                GL.Rotate(angle.Z, Vector3.UnitZ);
                GL.Translate(-pos.X, -pos.Y, -pos.Z);

                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                if (RendererProperties.RenderShaders)
                    Shaders.Update();
                Scene.Render();
                Shaders.SelectShader(VShaderTypes.None);
                if (RendererProperties.RenderWireframe) {
                    Scene.RenderDebug();
                }
                if (RendererProperties.ShowPages)
                    Cache.RenderDebug();
                Text.Render();
                if (showMap) {
                    RenderTexture(World.MapId);
                }
                Console.Render();
            }
        }

        private int r;

        private void RenderTexture(int id) {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, viewWidth, viewHeight, 0, 0.1f, 2048);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Translate(0, 0, -1.0f);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Fog);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.Fog);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);

            GL.Color3(1, 1, 1);

            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex3(0, viewHeight, 0);

            GL.TexCoord2(0, 1);
            GL.Vertex3(0, viewHeight - MAP_SIZE, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex3(MAP_SIZE, viewHeight - MAP_SIZE, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex3(MAP_SIZE, viewHeight, 0);
            GL.End();

            {
                r++;
                var c = ColorUtils.UniqueColor(r);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                var pos = Avatar.CameraPosition;
                pos /= (WorldUtils.WORLD_GRID * WorldUtils.REGION_SIZE);
                //pos.Y /= (WorldUtil.WORLD_GRID * WorldUtil.REGION_SIZE);
                pos *= MAP_SIZE;
                pos.Y += viewHeight - MAP_SIZE;
                GL.Color3((Color) c);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex3(pos.X, pos.Y, 0);
                GL.Vertex3(pos.X + 10, pos.Y, 0);
                GL.Vertex3(pos.X + 10, pos.Y + 10, 0);
                GL.Vertex3(pos.X, pos.Y + 10, 0);
                GL.End();
            }

            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void Update() { /* Do nothing */ }

        public void ToggleShowMap() {
            showMap = !showMap;
        }

        private bool renderLoadingScreen;
        private float loadingScreenProgress;

        public void RequestLoadingScreen(float progress) {
            renderLoadingScreen = true;
            loadingScreenProgress = progress;
        }

        private void RenderLoadingScreen() {
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Color3(1 - loadingScreenProgress, loadingScreenProgress, 0);
            GL.LineWidth(10.0f);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            CanvasBegin(-20, 120, 0, 100, 0);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(0, 50);
            GL.Vertex2(loadingScreenProgress * 100, 50);
            GL.End();
            CanvasEnd();
            Text.Render();
            Console.Render();
        }

        private void CanvasBegin(int left, int right, int bottom, int top, int size) {
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Fog);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);
            if (size != 0) {
                GL.Viewport(0, 0, size, size);
            } else {
                GL.Viewport(0, 0, viewWidth, viewHeight);
            }
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(left, right, bottom, top, 0.1f, 2048);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Translate(0, 0, -10);
        }

        private void CanvasEnd() {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

    }
}

/*
 *  From render.cpp

        #define RENDER_DISTANCE     1536
        #define NEAR_CLIP           0.2f
        #define FOV                 120

        static float view_aspect;
        static SDL_Surface* screen;
        static int max_dimension;

        static void draw_water(float tile)
        {

            int edge;

            edge = WorldUtil.REGION_SIZE * WorldUtil.WORLD_GRID;
            glBegin(GL_QUADS);
            glNormal3f(0, 0, 1);

            glTexCoord2f(0, 0);
            glVertex3i(edge, edge, 0);

            glTexCoord2f(0, -tile);
            glVertex3i(edge, 0, 0);

            glTexCoord2f(tile, -tile);
            glVertex3i(0, 0, 0);

            glTexCoord2f(tile, 0);
            glVertex3i(0, edge, 0);
            glEnd();


        }

        //void  water_map (bool underwater)
        //{

        //  GLtexture*      t;

        //  glBindTexture (GL_TEXTURE_2D, RegionMap ());
        //  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
        //  //glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
        //  glEnable (GL_TEXTURE_2D);
        //  glEnable (GL_BLEND);
        ////  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        //  glBlendFunc (GL_ONE, GL_ONE);
        //  glColor3f (1.0f, 1.0f, 1.0f);
        //  glDepthMask (false);
        //  draw_water (1);
        //  glDepthMask (true);
        //  return;
        //  if (!underwater) {
        //    t = TextureFromName ("water1.bmp", MASK_LUMINANCE);
        //    t = TextureFromName ("water.bmp");
        //    glBindTexture (GL_TEXTURE_2D, t->id);
        //  	//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
        //    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
        //    //glBlendFunc (GL_ZERO, GL_SRC_COLOR);
        //    glBlendFunc (GL_ONE, GL_ONE);
        //    glColor4f (1.0f, 1.0f, 1.0f, 1.0f);
        //    draw_water (256);
        //  }

        //}


        void RenderClick(int x, int y)
        {

            GLvector p;

            if (!showMap)
                return;
            y -= viewHeight - MAP_SIZE;
            if (y < 0 || x > MAP_SIZE)
                return;
            p.x = (float)x / MAP_SIZE;
            p.y = (float)y / MAP_SIZE;
            p.x *= WorldUtil.WORLD_GRID * WorldUtil.REGION_SIZE;
            p.y *= WorldUtil.WORLD_GRID * WorldUtil.REGION_SIZE;
            p.z = WorldUtil.REGION_SIZE;
            AvatarPositionSet(p);

        }

        void RenderCreate(int width, int height, int bits, bool fullscreen)
        {

            int flags;
            float fovy;
            int d;
            int size;

            ConsoleLog("RenderCreate: Creating %dx%d viewport", width, height);
            SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
            viewWidth = width;
            viewHeight = height;
            view_aspect = (float)width / (float)height;
            flags = SDL_OPENGL;
            if (fullscreen)
                flags |= SDL_FULLSCREEN;
            else
                flags |= SDL_RESIZABLE;
            screen = SDL_SetVideoMode(width, height, bits, flags);
            if (!screen)
                ConsoleLog("Unable to set video mode: %s\n", SDL_GetError());

            glMatrixMode(GL_PROJECTION);
            glLoadIdentity();
            fovy = FOV;
            if (view_aspect > 1.0f)
                fovy /= view_aspect;
            gluPerspective(fovy, view_aspect, NEAR_CLIP, RENDER_DISTANCE);
            //gluPerspective (fovy, view_aspect, 0.1f, 400);
            glMatrixMode(GL_MODELVIEW);
            size = min(width, height);
            d = 128;
            while (d < size)
            {
                max_dimension = d;
                d *= 2;
            }
            TexturePurge();
            SceneTexturePurge();
            WorldTexturePurge();
            CgCompile();
            TextCreate(width, height);

        }

        //Return the power of 2 closest to the smallest dimension of the canvas
        //(This tells you how much room you have for drawing on textures.)
        int RenderMaxDimension()
        {

            //return 64;///////////////////////////////////////////
            return max_dimension;

        }

        static float spin;
  */
