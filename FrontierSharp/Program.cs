using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierSharp {
	internal class Program {
		private static void Main(string[] args) {
			using (var frontier = new Frontier()) {

				frontier.Run(30.0);

			}
		}
	}
}
