/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: CVar.h 201 2012-10-17 18:43:27Z effer $
*/

namespace CVars {
	using System.IO;
	using Interfaces;

	delegate void SerialisationFunction<T>(StringWriter sw, T val);
	delegate void DeserialisationFunction<T>(StringReader sr, T val);

	class CVar<T> : CVar where T : ICVarValue {
		public T VarData { get; set; }

		private SerialisationFunction<T> Serialisation;
		private DeserialisationFunction<T> Deserialisation;

		///<summary>
		/// If serialise false, this CVar will not be taken into account when serialising (eg saving) the Trie
		///</summary>
		public CVar(string varName, T varValue, string help = "No help available", bool serialise = true,
				SerialisationFunction<T> serialisation = null, DeserialisationFunction<T> deserialisation = null)
				: base(varName, help, serialise) {
			Serialisation = serialisation;
			Deserialisation = deserialisation;
			VarData = varValue;
		}

		///<summary>
		/// Convert value to string representation
		/// Call the original function that was installed at object creation time,
		/// regardless of current object class type T.
		///</summary>
		public string GetValueAsString() {
			if (Serialisation != null) {
				using (var sw = new StringWriter()) {
					Serialisation(sw, VarData);
					return sw.ToString();
				}
			} else {
				return VarData.ToString();
			}
		}

		// Convert string representation to value
		// Call the original function that was installed at object creation time,
		// regardless of current object class type T.
		void SetValueFromString(string value) {
			if (Deserialisation != null) {
				using (var sr = new StringReader(value)) {
					Deserialisation(sr, VarData);
				}
			} else {
				VarData.Parse(value);
			}
		}
	}
}