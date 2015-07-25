namespace CVars.CVarTypes {
	internal class IntVar : Interfaces.ICVarValue {
		private int Value { get; set; }

		public IntVar(int i) {
			Value = i;
		}

		public void Parse(string s) {
			Value = int.Parse(s);
		}

		public override string ToString() {
			return Value.ToString();
		}

		public static implicit operator int (IntVar var)
		{
			return var.Value;
		}

		public static implicit operator IntVar (int i) {
			return new IntVar(i);
		}
	}
}
