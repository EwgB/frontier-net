namespace FrontierSharp {
    using System;
    using System.ComponentModel;
    using System.Drawing;

    using NLog;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;
    using OpenTK.Input;

    using Common;
    using Common.Avatar;
    using Common.Environment;
    using Common.Particles;
    using Common.Renderer;
    using Common.Scene;
    using Common.Shaders;
    using Common.Textures;
    using Common.World;

    internal class Frontier : GameWindow, IModule {

        #region Constants

        private const double UPDATE_INTERVAL = 15 / 1000f;
        
        #endregion

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #region Modules

        private readonly IAvatar avatar;
        private readonly ICache cache;
        private readonly IConsole console;
        private readonly IEnvironment environment;
        private readonly IGame game;
        private readonly IParticles particles;
        private readonly IPlayer player;
        private readonly IRenderer renderer;
        private readonly IScene scene;
        private readonly IShaders shaders;
        private readonly ISky sky;
        private readonly IText text;
        private readonly ITextures texture;
        private readonly IWorld world;

        #endregion

        public Frontier(
                        IAvatar avatar,
                        ICache cache,
                        IConsole console,
                        IEnvironment environment,
                        IGame game,
                        IParticles particles,
                        IPlayer player,
                        IRenderer renderer,
                        IScene scene,
                        IShaders shaders,
                        ISky sky,
                        IText text,
                        ITextures texture,
                        IWorld world) {
            this.avatar = avatar;
            this.cache = cache;
            this.console = console;
            this.environment = environment;
            this.game = game;
            this.particles = particles;
            this.player = player;
            this.renderer = renderer;
            this.scene = scene;
            this.shaders = shaders;
            this.sky = sky;
            this.text = text;
            this.texture = texture;
            this.world = world;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Trace("OnLoad");
            Init();
        }

        protected override void OnDisposed(EventArgs e) {
            base.OnDisposed(e);

            this.game.Dispose();
            this.texture.Dispose();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            Log.Trace("OnResize");

            GL.Viewport(this.ClientRectangle.X, this.ClientRectangle.Y, this.ClientRectangle.Width, this.ClientRectangle.Height);
            var projection = Matrix4.CreatePerspectiveFieldOfView((float) Math.PI / 4, this.Width / (float) this.Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            base.OnRenderFrame(e);
            Log.Trace("OnRenderFrame");

            this.renderer.Render();

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            base.OnUpdateFrame(e);
            Log.Trace("OnUpdateFrame");

            this.console.Update();
            Update();
            this.game.Update();
            this.avatar.Update();
            this.player.Update();
            this.environment.Update();
            this.sky.Update();
            this.scene.Update(UPDATE_INTERVAL);
            this.cache.Update(UPDATE_INTERVAL);
            this.particles.Update();
            this.renderer.Update();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e) {
            base.OnKeyDown(e);
            Log.Trace("OnKeyDown");

            Log.Info("Button {0} pushed.", e.Key);

            // Process the key to toggle the console first, so that, if the console is open,
            // we can pass all other input to it
            if (Key.Grave == e.Key) {
                this.console.ToggleConsole();
            } else if (this.console.IsOpen) {
                this.console.ProcessKey(e);
            } else {
                switch (e.Key) {
                    case Key.Escape:
                        Close();
                        break;
                    case Key.Tab:
                        this.renderer.ToggleShowMap();
                        break;
                }
            }
        }

        public void Init() {
            Log.Info("Init start...");

            this.Title = "Frontier";
            //Icon = new Icon("Resources/icon.bmp");
            this.Size = new Size(1400, 800);

            this.Keyboard.KeyRepeat = true;

            this.console.Init();
            this.particles.Init();
            this.environment.Init();
            this.renderer.Init();
            this.shaders.Init();
            this.game.Init();
            this.player.Init();
            this.avatar.Init();
            this.texture.Init();
            this.world.Init();
            this.scene.Init();
            this.sky.Init();
            this.text.Init();

            Log.Info("Init done.");
        }

        public void Update() {
            /*
            SDL_Event event;
            long      now;

            while (SDL_PollEvent(&event)) { 
                switch(event.type){ 
                case SDL_JOYAXISMOTION:
                    InputJoystickSet (event.jaxis.axis, event.jaxis.value);
                    break;
                case SDL_KEYUP:
                    InputKeyUp (event.key.keysym.sym);
                    break;
                case SDL_MOUSEBUTTONDOWN:
                    if (event.button.button == SDL_BUTTON_RIGHT) {
                        InputMouselookSet (!InputMouselook ());
                        SDL_ShowCursor (false);
                        SDL_WM_GrabInput (SDL_GRAB_ON);
                    }
                    if(event.button.button == SDL_BUTTON_WHEELUP)
                        InputKeyDown (INPUT_MWHEEL_UP);
                    if(event.button.button == SDL_BUTTON_WHEELDOWN)
                        InputKeyDown (INPUT_MWHEEL_DOWN);
                    if (event.button.button == SDL_BUTTON_LEFT && !InputMouselook ())
                        RenderClick (event.motion.x, event.motion.y);        
                    break;
                case SDL_MOUSEBUTTONUP:
                    if (event.button.button == SDL_BUTTON_LEFT)
                        lmb = false;
                    else if (event.button.button == SDL_BUTTON_MIDDLE)
                        mmb = false;
                    if (InputMouselook ())
                        SDL_ShowCursor (false);
                    else { 
                        SDL_ShowCursor (true);
                        SDL_WM_GrabInput (SDL_GRAB_OFF);
                    }
                    if(event.button.button == SDL_BUTTON_WHEELUP)
                        InputKeyUp (INPUT_MWHEEL_UP);
                    if(event.button.button == SDL_BUTTON_WHEELDOWN)
                        InputKeyUp (INPUT_MWHEEL_DOWN);
                    break;
                case SDL_MOUSEMOTION:
                    if (InputMouselook ()) 
                        AvatarLook (event.motion.yrel, -event.motion.xrel);
                    break;
                case SDL_VIDEORESIZE: //User resized window
                    center_x = event.resize.w / 2;
                    center_y = event.resize.h / 2;
                    RenderCreate (event.resize.w, event.resize.h, 32, false);
                    break; 
                } //Finished with current event

            } //Done with all events for now
            now = SDL_GetTicks ();
            elapsed = now - last_update;
            elapsed_seconds = (float)elapsed / 1000.0f;
            last_update = now;
            */
        }
    }
}
