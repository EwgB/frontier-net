/*-----------------------------------------------------------------------------
  FMath.cs
  2009 Shamus Young, 2012 Ewgenij Belzmann
-------------------------------------------------------------------------------
  Various useful math functions.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Frontier {
	class FMath {
		private const float
			FREEZING = 0.32f,
			TEMP_COLD = 0.45f,
			TEMP_TEMPERATE = 0.6f,
			TEMP_HOT = 0.9f,
			MIN_TEMP = 0.0f,
			MAX_TEMP = 1.0f,
			DEGREES_TO_RADIANS = .017453292f,
			RADIANS_TO_DEGREES = 57.29577951f,
			NEGLIGIBLE = 0.000000000001f,
			//PI = (3.1415926535f),
			GRAVITY = 9.5f,
			// This is used to scale the z value of normals
			// Lower numbers make the normals more extreme, exaggerate the lighting
			NORMAL_SCALING = 0.6f;

		// Keep an angle between 0 and 360
		public static float MathAngle(float angle) {
			if (angle < 0.0f)
				angle = 360.0f - (Math.Abs(angle) % 360);
			else
				angle = angle % 360;
			return angle;
		}

		// Get an angle between two given points on a grid
		public static float MathAngle(float x1, float y1, float x2, float y2) {
			float x_delta, z_delta, angle;

			z_delta = (y1 - y2);
			x_delta = (x1 - x2);

			if (x_delta == 0)
				return ((z_delta > 0) ? 0.0f : 180.0f);

			if (Math.Abs(x_delta) < Math.Abs(z_delta)) {
				angle = 90 - (float) Math.Atan(z_delta / x_delta) * RADIANS_TO_DEGREES;
				if (x_delta < 0)
					angle -= 180.0f;
			} else {
				angle = (float) Math.Atan(x_delta / z_delta) * RADIANS_TO_DEGREES;
				if (z_delta < 0.0f)
					angle += 180.0f;
			}

			if (angle < 0.0f)
				angle += 360.0f;

			return angle;
		}

		// Get distance (squared) between 2 points on a plane
		public static float MathDistance2(float x1, float y1, float x2, float y2) {
			float dx, dy;

			dx = x1 - x2;
			dy = y1 - y2;
			return dx * dx + dy * dy;
		}

		// Get distance between 2 points on a plane. This is slightly slower than MathDistance2
		public static float MathDistance(float x1, float y1, float x2, float y2) {
			float dx, dy;

			dx = x1 - x2;
			dy = y1 - y2;
			return (float) Math.Sqrt(dx * dx + dy * dy);
		}

		// Difference between two angles
		public static float MathAngleDifference(float a1, float a2) {
			float result;

			result = (a1 - a2) % 360;
			if (result > 180.0)
				return result - 360.0F;
			if (result < -180.0)
				return result + 360.0F;
			return result;
		}

		// Interpolate between two values
		public static float MathInterpolate(float n1, float n2, float delta) {
			return n1 * (1.0f - delta) + n2 * delta;
		}

		// Return a scalar of 0.0 to 1.0, based an the given values position within a range
		public static float MathSmoothStep(float val, float a, float b) {
			if (b == a)
				return 0.0f;
			val -= a;
			val /= (b - a);
			return Math.Min(Math.Max(val, 0.0f), 1.0f);
		}

		// Average two values
		public static float MathAverage(float n1, float n2) {
			return (n1 + n2) / 2.0f;
		}

		/* This will take linear input values from 0.0 to 1.0 and convert them to values along a curve.
		 * This could also be acomplished with sin(), but this way avoids converting to radians and back. */
		public static float MathScalarCurve(float val) {
			float sign;

			val = (val - 0.5f) * 2.0f;
			sign = ((val < 0.0f) ? -1.0f : 1.0f);

			if (val < 0.0f)
				val = -val;

			val = 1.0f - val;
			val *= val;
			val = 1.0f - val;
			val *= sign;
			val = (val + 1.0f) / 2.0f;

			return val;
		}

		// This will take values between low and high and return a value from 0 to 1.
		public static float MathScalar(float val, float low, float high) {
			val = Math.Max(val, low);
			val = Math.Min(val, high);
			return (val - low) / (high - low);
		}

		/* This forms a theoretical quad with the four elevation values. Given the offset from the upper-left corner,
		 * it determines what the elevation should be at that point in the center area. "left" determines if the
		 * quad is cut from y2 to y1, or from y0 to y3.
		 * 
		 * y0-----y1
		 *  |     |
		 *  |     |
		 * y2-----y3
		 */
		public static float MathInterpolateQuad(float y0, float y1, float y2, float y3, Vector2 offset, bool left) {
			float   a;
			float   b;
			float   c;

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

		/*
		public static float MathInterpolateQuad (float y0, float y1, float y2, float y3, Vector2 offset) {
			MathInterpolateQuad (y0, y1, y2, y3, offset, false);
		} */
	}
}
