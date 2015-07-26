namespace CVars {
	internal abstract class CVar {
		internal string VarName { get; private set; }
		internal bool Serialise { get; private set; }
		protected string Help { get; private set; }

		internal abstract string GetValueAsString();
		internal abstract void SetValueFromString(string value);

		internal CVar(string varName, string help, bool serialise) {
			VarName = varName;
			Serialise = serialise;
			Help = help;
		}
	}
}
