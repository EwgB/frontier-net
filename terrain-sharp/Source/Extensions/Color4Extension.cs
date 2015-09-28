namespace terrain_sharp.Source.Extensions {
	using System;
	using OpenTK.Graphics;

	public static class Color4Extension {
		public static Color4 Add(this Color4 c1, Color4 c2) {
			return new Color4(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B, c1.A);
		}

		public static Color4 Add(this Color4 color, float delta) {
			return new Color4(color.R + delta, color.G + delta, color.B + delta, color.A);
		}

		public static Color4 Substract(this Color4 c1, Color4 c2) {
			return new Color4(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B, c1.A);
		}

		public static Color4 Multiply(this Color4 c1, Color4 c2) {
			return new Color4(c1.R * c2.R, c1.G * c2.G, c1.B * c2.B, c1.A);
		}

		public static Color4 Scale(this Color4 color, float scale) {
			return new Color4(color.R * scale, color.G * scale, color.B * scale, color.A);
		}

		public static bool Compare(this Color4 c1, Color4 c2) {
			return (c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);
		}

		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T> {
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		public static void Clamp(this Color4 color) {
			color.R.Clamp(0, 1);
			color.G.Clamp(0, 1);
			color.B.Clamp(0, 1);
			color.A.Clamp(0, 1);
		}

		public static void Normalize(this Color4 color) {
			float n = Math.Max(color.R, Math.Max(color.G, color.B));
			if (n > 1.0f) {
				color.R /= n;
				color.G /= n;
				color.B /= n;
			}
		}

		public static float Brightness(this Color4 color) {
			return (color.R + color.B + color.G) / 3;
		}
	}
}
