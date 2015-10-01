namespace terrain_sharp.Source.Utils {
	using System;
	using OpenTK;

	public static class MathUtils {
		///<summary>Keep an angle between 0 and 360</summary>
		public static float Angle(float angle) {
			if (angle < 0)
				angle = 360 - Math.Abs(angle) % 360;
			else
				angle %= 360;
			return angle;
		}

		///<summary>Get an angle between two given points on a grid</summary>
		public static float Angle(float x1, float y1, float x2, float y2) {
			float z_delta = (y1 - y2);
			float x_delta = (x1 - x2);
			if (x_delta == 0) {
				if (z_delta > 0)
					return 0;
				else
					return 180;
			}
			float angle;
			if (Math.Abs(x_delta) < Math.Abs(z_delta)) {
				angle = 90 - MathHelper.RadiansToDegrees((float) Math.Atan(z_delta / x_delta));
        if (x_delta < 0)
					angle -= 180;
			} else {
				angle = MathHelper.RadiansToDegrees((float) Math.Atan(x_delta / z_delta));
				if (z_delta < 0)
					angle += 180;
			}
			if (angle < 0)
				angle += 360;
			return angle;
		}

		///<summary>Get distance (squared) between 2 points on a plane</summary>
		public static float Distance2(float x1, float y1, float x2, float y2) {
			float dx = x1 - x2;
			float dy = y1 - y2;
			return dx * dx + dy * dy;
		}

		///<summary>Get distance between 2 points on a plane. This is slightly slower than MathDistance2</summary>
		public static float Distance(float x1, float y1, float x2, float y2) {
			float dx = x1 - x2;
			float dy = y1 - y2;
			return (float) Math.Sqrt(dx * dx + dy * dy);
		}

		///<summary>Difference between two angles</summary>
		public static float AngleDifference(float a1, float a2) {
			float result;

			result = (a1 - a2) % 360;
			if (result > 180.0)
				return result - 360;
			if (result < -180.0)
				return result + 360;
			return result;
		}

		/// <summary>Interpolate between two values</summary>
		public static float Interpolate(float n1, float n2, float delta) {
			return n1 * (1 - delta) + n2 * delta;
		}

		///<summary>return a scalar of 0.0 to 1.0, based an the given values position within a range</summary>
		public static float SmoothStep(float val, float a, float b) {
			if (b == a)
				return 0;
			val -= a;
			val /= (b - a);
			return MathHelper.Clamp(val, 0, 1);
		}

		///<summary>Average two values</summary>
		public static float Average(float n1, float n2) {
			return (n1 + n2) / 2;
		}

		///<summary>
		///	This will take linear input values from 0.0 to 1.0 and convert them to 
		///	values along a curve.  This could also be acomplished with sin (), but this 
		///	way avoids converting to radians and back.
		///</summary>
		public static float ScalarCurve(float val) {
			int sign;
			val = (val - 0.5f) * 2;
			if (val < 0)
				sign = -1;
			else
				sign = 1;
			if (val < 0)
				val = -val;
			val = 1 - val;
			val *= val;
			val = 1 - val;
			val *= sign;
			val = (val + 1) / 2;
			return val;
		}

		///<summary>This will take values between low and high and return a value from 0 to 1.</summary>
		public static float Scalar(float val, float low, float high) {
			val = Math.Max(val, low);
			val = Math.Min(val, high);
			return (val - low) / (high - low);
		}

		///<summary>
		///	This forms a theoretical quad with the four elevation values. Given the 
		///	offset from the upper-left corner, it determines what the elevation
		///	should be at that point in the center area.
		///
		///	y0-----y1
		///	 |     |
		///	 |     |
		///	y2-----y3
		///</summary>
		///<param name="left">Determines if the quad is cut from y2 to y1, or from y0 to y3.</param>
		public static float InterpolateQuad(float y0, float y1, float y2, float y3, Vector2 offset, bool left = false) {
			float a, b, c;

			if (left) {
				if (offset.X + offset.Y < 1) {
					c = y2 - y0;
					b = y1 - y0;
					a = y0;
				} else {
					c = y3 - y1;
					b = y3 - y2;
					a = y3 - (b + c);
				}
			} else { //right
				if (offset.X < offset.Y) {
					c = y2 - y0;
					b = y3 - y2;
					a = y0;
				} else {
					c = y3 - y1;
					b = y1 - y0;
					a = y0;
				}
			}
			return (a + b * offset.X + c * offset.Y);
		}

		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T> {
      if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}
	}
}
