namespace terrain_sharp.Source.Utils {
	using System;

	public static class MathUtils {
		public static double DegreeToRadian(double angle) {
			return Math.PI * angle / 180;
		}

		public static double RadianToDegree(double angle) {
			return angle * (180 / Math.PI);
		}
		
		///<summary>Keep an angle between 0 and 360</summary>
		public static float MathAngle(float angle) {
			if (angle < 0)
				angle = 360 - Math.Abs(angle) % 360;
			else
				angle %= 360;
			return angle;
		}

		///<summary>Get an angle between two given points on a grid</summary>
		public static float MathAngle(float x1, float y1, float x2, float y2) {
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
				angle = 90 - (float) RadianToDegree(Math.Atan(z_delta / x_delta));
				if (x_delta < 0)
					angle -= 180;
			} else {
				angle = (float) RadianToDegree(Math.Atan(x_delta / z_delta));
				if (z_delta < 0)
					angle += 180;
			}
			if (angle < 0)
				angle += 360;
			return angle;
		}

		///*-----------------------------------------------------------------------------
		//Get distance (squared) between 2 points on a plane
		//-----------------------------------------------------------------------------*/

		//float MathDistance2(float x1, float y1, float x2, float y2) {

		//	float dx;
		//	float dy;

		//	dx = x1 - x2;
		//	dy = y1 - y2;
		//	return dx * dx + dy * dy;

		//}

		///*-----------------------------------------------------------------------------
		//Get distance between 2 points on a plane. This is slightly slower than 
		//MathDistance2 ()
		//-----------------------------------------------------------------------------*/

		//float MathDistance(float x1, float y1, float x2, float y2) {

		//	float dx;
		//	float dy;

		//	dx = x1 - x2;
		//	dy = y1 - y2;
		//	return (float) sqrt(dx * dx + dy * dy);

		//}

		///*-----------------------------------------------------------------------------
		//difference between two angles
		//-----------------------------------------------------------------------------*/

		//float MathAngleDifference(float a1, float a2) {

		//	float result;

		//	result = (float) fmod(a1 - a2, 360.0f);
		//	if (result > 180.0)
		//		return result - 360.0F;
		//	if (result < -180.0)
		//		return result + 360.0F;
		//	return result;

		//}

		/// <summary>Interpolate between two values</summary>
		static internal float Interpolate(float n1, float n2, float delta) {
			return n1 * (1.0f - delta) + n2 * delta;
		}

		///*-----------------------------------------------------------------------------
		//return a scalar of 0.0 to 1.0, based an the given values position within a range
		//-----------------------------------------------------------------------------*/

		//float MathSmoothStep(float val, float a, float b) {

		//	if (b == a)
		//		return 0.0f;
		//	val -= a;
		//	val /= (b - a);
		//	return clamp(val, 0.0f, 1.0f);

		//}

		///*-----------------------------------------------------------------------------
		//Average two values
		//-----------------------------------------------------------------------------*/

		//float MathAverage(float n1, float n2) {

		//	return (n1 + n2) / 2.0f;

		//}

		///*-----------------------------------------------------------------------------
		//	This will take linear input values from 0.0 to 1.0 and convert them to 
		//	values along a curve.  This could also be acomplished with sin (), but this 
		//	way avoids converting to radians and back.
		//-----------------------------------------------------------------------------*/

		//float MathScalarCurve(float val) {

		//	float sign;

		//	val = (val - 0.5f) * 2.0f;
		//	if (val < 0.0f)
		//		sign = -1.0f;
		//	else
		//		sign = 1.0f;
		//	if (val < 0.0f)
		//		val = -val;
		//	val = 1.0f - val;
		//	val *= val;
		//	val = 1.0f - val;
		//	val *= sign;
		//	val = (val + 1.0f) / 2.0f;
		//	return val;

		//}

		///*-----------------------------------------------------------------------------
		//	This will take values between low and high and return a value from 0 to 1.
		//-----------------------------------------------------------------------------*/

		//float MathScalar(float val, float low, float high) {

		//	val = max(val, low);
		//	val = min(val, high);
		//	return (val - low) / (high - low);

		//}


		///*-----------------------------------------------------------------------------

		//	This forms a theoretical quad with the four elevation values.  Given the 
		//	offset from the upper-left corner, it determines what the elevation
		//	should be at that point in the center area.  left" determines if the 
		//	quad is cut from y2 to y1, or from y0 to y3.

		//	y0-----y1
		//	 |     |
		//	 |     |
		//	y2-----y3

		//-----------------------------------------------------------------------------*/

		//float MathInterpolateQuad(float y0, float y1, float y2, float y3, GLvector2 offset, bool left) {

		//	float a;
		//	float b;
		//	float c;

		//	if (left) {
		//		if (offset.x + offset.y < 1) {
		//			c = y2 - y0;
		//			b = y1 - y0;
		//			a = y0;
		//		} else {
		//			c = y3 - y1;
		//			b = y3 - y2;
		//			a = y3 - (b + c);
		//		}
		//	} else { //right
		//		if (offset.x < offset.y) {
		//			c = y2 - y0;
		//			b = y3 - y2;
		//			a = y0;
		//		} else {
		//			c = y3 - y1;
		//			b = y1 - y0;
		//			a = y0;
		//		}
		//	}
		//	return (a + b * offset.x + c * offset.y);

		//}
		/*
		float MathInterpolateQuad (float y0, float y1, float y2, float y3, GLvector2 offset)
		{

			MathInterpolateQuad (y0, y1, y2, y3, offset, false);
			*/

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
