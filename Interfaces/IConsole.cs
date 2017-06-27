using OpenTK.Input;

namespace FrontierSharp.Interfaces {
    public interface IConsole : IModule {
        bool IsOpen { get; }

        void ToggleConsole();
        void ProcessKey(KeyboardKeyEventArgs e);
    }
}
