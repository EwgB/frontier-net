namespace FrontierSharp.Input {
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using OpenTK.Input;

    using Common.Input;


    internal class DummyInput : IInput {
        //public JoystickAxisCollection Joystick => JoystickAxisCollection { };
        private readonly IDictionary<int, float> joystick = new Dictionary<int, float> {
            { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };
        public IReadOnlyDictionary<int, float> Joystick => new ReadOnlyDictionary<int, float>(joystick);

        public bool Mouselook { get; set; }
        public bool MouseWheelDown { get; set; }
        public bool MouseWheelUp { get; set; }

        public void KeyDown(Key key) { /* Do nothing */ }

        public bool KeyPressed(Key key) => false;

        public bool KeyState(Key key) => false;

        public void KeyUp(Key key) { /* Do nothing */ }
    }
}
