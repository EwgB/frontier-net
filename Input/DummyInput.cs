namespace FrontierSharp.Input {
    using Common.Input;

    using OpenTK.Input;

    internal class DummyInput : IInput {
        public JoystickAxisCollection Joystick => null;

        public bool Mouselook { get; set; }
        public bool MouseWheelDown { get; set; }
        public bool MouseWheelUp { get; set; }

        public void KeyDown(Key key) { /* Do nothing */ }

        public bool KeyPressed(Key key) => false;

        public bool KeyState(Key key) => false;

        public void KeyUp(Key key) { /* Do nothing */ }
    }
}
