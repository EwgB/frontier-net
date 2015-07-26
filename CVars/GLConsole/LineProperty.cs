namespace CVars.GLConsole {
	///<summary>
	/// The type of line entered. Used to determine how each line is treated.
	///</summary>
	internal enum LineProperty {
		Log,				// text coming from a text being logged to the console
		Command,		// command entered at the console
		Function,		// a function
		Error,			// an error
		Help				//help text
	}
}
