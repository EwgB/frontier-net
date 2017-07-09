namespace FrontierSharp.Renderer {
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System;
    using System.Drawing;

    using Common.Avatar;
    using Common.Environment;
    using Common.Property;
    using Common.Renderer;
    using Common.Scene;
    using Common.Util;
    using Common.World;

    public class RendererImpl : IRenderer {
        // Constants
        private const int MAP_SIZE = 512;

        // Dependencies (injected via Ninject)
        private readonly IAvatar avatar;
        private readonly IWorld world;
        private readonly IEnvironment environment;
        private readonly IScene scene;

        private Color3 currentAmbient = Color3.Black;
        private Color3 currentDiffuse = Color3.White;
        private Color3 currentFog = Color3.White;

        private bool showMap;
        private int viewWidth;
        private int viewHeight;
        private Range<float> fog;

        private readonly IRendererProperties properties = new RendererProperties();
        public IProperties Properties { get { return this.properties; } }
        public IRendererProperties RendererProperties { get { return this.properties; } }

        public RendererImpl(IAvatar avatar, IWorld world, IEnvironment environment, IScene scene) {
            // Set dependencies
            this.avatar = avatar;
            this.world = world;
            this.environment = environment;
            this.scene = scene;
        }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Render() {
            EnvironmentData envData = this.environment.Current;
            Vector3 pos = this.avatar.CameraPosition;
            float waterLevel = Math.Max(this.world.GetWaterLevel(new Vector2(pos.X, pos.Y)), 0);

            if (pos.Z >= waterLevel) {
                //currentFog = (currentDiffuse + Color3.Blue) / 2;
                //GL.Fog(FogParameter.FogStart, RENDER_DISTANCE / 2);   // Fog Start Depth
                //GL.Fog(FogParameter.FogEnd, RENDER_DISTANCE);			// Fog End Depth
                GL.Fog(FogParameter.FogStart, envData.Fog.Min);         // Fog Start Depth
                GL.Fog(FogParameter.FogEnd, envData.Fog.Max);           // Fog End Depth
            } else {
                //cfog = new Color3(0.0f, 0.5f, 0.8f);
                GL.Fog(FogParameter.FogStart, 1);               // Fog Start Depth
                GL.Fog(FogParameter.FogEnd, 32);				// Fog End Depth
            }

            GL.Enable(EnableCap.Fog);
            GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
            //GL.Fog (FogParameter.FogMode, (int) FogMode.Exp);
            GL.Fog(FogParameter.FogColor, envData.Color[ColorTypes.Fog].R);
            GL.ClearColor((Color) envData.Color[ColorTypes.Fog]);
            //GL.ClearColor (0, 0, 0, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //GL.Clear (ClearBufferMask.DepthBufferBit);

            float[] light = new float[4];

            light[0] = -envData.Light.X;
            light[1] = -envData.Light.Y;
            light[2] = -envData.Light.Z;
            light[3] = 0.0f;

            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.Lighting);
            currentAmbient = Color3.Black;
            GL.Light(LightName.Light1, LightParameter.Ambient, envData.Color[ColorTypes.Ambient].R);
            Color3 c = envData.Color[ColorTypes.Light];
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
            Vector3 angle = this.avatar.CameraAngle;
            GL.Rotate(angle.X, Vector3.UnitX);
            GL.Rotate(angle.Y, Vector3.UnitY);
            GL.Rotate(angle.Z, Vector3.UnitZ);
            GL.Translate(-pos.X, -pos.Y, -pos.Z);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //if (this.properties.RenderShaders)
            //    CgUpdate();
            this.scene.Render();
            //CgShaderSelect(VSHADER_NONE);
            if (this.properties.RenderWireframe) {
                this.scene.RenderDebug();
            }
            //if (this.properties.ShowPages)
            //    CacheRenderDebug();
            //TextRender();
            if (showMap) {
                RenderTexture(this.world.MapId);
            }
            //ConsoleRender();
        }

        private int r = 0;
        private void RenderTexture(uint id) {
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
                Color3 c = ColorUtils.UniqueColor(r);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Vector3 pos = this.avatar.CameraPosition;
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

        public void Update() {
            var envData = this.environment.Current;

            this.currentDiffuse = envData.Color[ColorTypes.Light];
            this.currentAmbient = envData.Color[ColorTypes.Ambient];
            this.currentFog = envData.Color[ColorTypes.Fog];
            this.fog = envData.Fog;
        }

        public void ToggleShowMap() {
            showMap = !showMap;
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

        void RenderCanvasBegin(int left, int right, int bottom, int top, int size)
        {

            glDisable(GL_CULL_FACE);
            glDisable(GL_FOG);
            glDisable(GL_DEPTH_TEST);
            glDisable(GL_LIGHTING);
            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            glEnable(GL_TEXTURE_2D);
            if (size)
                glViewport(0, 0, size, size);
            else
                glViewport(0, 0, viewWidth, viewHeight);
            glMatrixMode(GL_PROJECTION);
            glPushMatrix();
            glLoadIdentity();
            glOrtho(left, right, bottom, top, 0.1f, 2048);
            glMatrixMode(GL_MODELVIEW);
            glPushMatrix();
            glLoadIdentity();
            glTranslate(0, 0, -10.0f);

        }

        void RenderCanvasEnd()
        {

            glMatrixMode(GL_PROJECTION);
            glPopMatrix();
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glMatrixMode(GL_MODELVIEW);
            glPopMatrix();

        }

        void RenderInit(void)
        {

            currentAmbient = glRgba(0.0f);
            currentDiffuse = glRgba(1.0f);
            currentFog = glRgba(1.0f);
            fogMax = 1000;
            fogMin = 1;
            CgInit();

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

        void RenderLoadingScreen(float progress)
        {

            glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            glColor3f(1.0f - progress, progress, 0.0f);
            glLineWidth(10.0f);
            glDisable(GL_LIGHTING);
            glDisable(GL_TEXTURE_2D);
            glDisable(GL_BLEND);
            glBindTexture(GL_TEXTURE_2D, 0);
            RenderCanvasBegin(-20, 120, 0, 100, 0);
            glBegin(GL_LINES);
            glVertex2f(0, 50);
            glVertex2f(progress * 100, 50);
            glEnd();
            RenderCanvasEnd();
            TextRender();
            ConsoleRender();
            SDL_GL_SwapBuffers();

        }

        //if (0) { //water reflection effect.  Needs stencil buffer to work right
        //  glDisable (GL_FOG);
        //  glPushMatrix ();
        //  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("water4.bmp"));
        //  glColorMask (false, false, false, true);
        //  draw_water (256);
        //  glColorMask (true, true, true, false);
        //  glLoadIdentity();
        //  pos = CameraPosition ();
        //  glScalef (1, -1, -1);
        //  //pos *= -1;
        //  angle = CameraAngle ();
        //  glRotatef (angle.x, -1.0f, 0.0f, 0.0f);
        //  glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
        //  glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
        //  glTranslate (-pos.x, -pos.y, pos.z);
        //  glDepthFunc (GL_GREATER);
        // // glScalef (1, -1, 1);
        //  glFrontFace (GL_CW);
        //  glPolygonMode(GL_BACK, GL_FILL);
        //  //glPolygonMode(GL_FRONT, GL_POINT);
        //  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        //  glDisable (GL_CULL_FACE);
        //  SceneRender ();
        //  glDepthFunc (GL_LEQUAL);
        //  glPopMatrix ();
        //  glFrontFace (GL_CCW);
        //  //glEnable (GL_BLEND);
        //  //glBlendFunc (GL_ONE, GL_ONE);
        //  //glDisable (GL_LIGHTING);
        //  //glColor4f (1.0f, 1.0f, 1.0f, 0.5f);
        //  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("water4.bmp"));
        //  glColorMask (false, false, false, false);
        //  draw_water (256);
        //  glColorMask (true, true, true, false);
        //  //draw_water (256);
        //  glEnable (GL_LIGHTING);
        //  glPolygonMode(GL_FRONT, GL_FILL);
        //  glPolygonMode(GL_BACK, GL_LINE);

        //}
  */
