namespace FrontierSharp.Common {
    using OpenTK.Input;

    /// <summary>Runs a "quake-like" console.</summary>
    public interface IConsole : IModule {
        bool IsOpen { get; }

        void ToggleConsole();
        void ProcessKey(KeyboardKeyEventArgs e);
        void Render();
    }
}
