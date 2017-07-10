namespace FrontierSharp.Common.Input {
    using OpenTK.Input;

    public interface IInput {
        void KeyDown(Key key);
        void KeyUp(Key key);
        bool KeyState(Key key);
        bool KeyPressed(Key key);

        bool Mouselook { get; set; }
        bool MouseWheelUp { get; set; }
        bool MouseWheelDown { get; set; }

        JoystickAxisCollection Joystick { get; }
    }
}
