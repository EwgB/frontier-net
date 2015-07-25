namespace CVars {
	class CVar {
		public string VarName { get; private set; }
		public bool Serialise { get; private set; }
		protected string Help { get; private set; }

		public CVar(string varName, string help, bool serialise) {
			VarName = varName;
			Serialise = serialise;
			Help = help;
		}
	}
}
