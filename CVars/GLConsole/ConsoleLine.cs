namespace CVars.GLConsole {
	///<summary>
	///  A line of text contained in the console can be either inputted commands or
	///  log text from the application.
	///</summary>
	internal class ConsoleLine {
		// The actual text
		internal string Text { get; private set; }
		// See LineProperty
		internal LineProperty Options { get; private set; }
		// display on the console screen?
		internal bool Display { get; private set; }

		public ConsoleLine(string text, LineProperty options = LineProperty.Log, bool display = true) {
			Text = text;
			Options = options;
			Display = display;
		}
	}
}
