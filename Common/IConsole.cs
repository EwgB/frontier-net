using OpenTK.Input;

namespace FrontierSharp.Common {
    public interface IConsole : IModule {
        bool IsOpen { get; }

        void ToggleConsole();
        void ProcessKey(KeyboardKeyEventArgs e);
    }
}
