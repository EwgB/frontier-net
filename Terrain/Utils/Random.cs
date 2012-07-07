/*-----------------------------------------------------------------------------
									               r a n d o m
-----------------------------------------------------------------------------*/

/*-----------------------------------------------------------------------------
  The Mersenne Twister by Matsumoto and Nishimura <matumoto@math.keio.ac.jp>.
  It sets new standards for the period, quality and speed of random number
  generators. The incredible period is 2^19937 - 1, a number with about 6000
  digits; the 32-bit random numbers exhibit best possible equidistribution
  properties in dimensions up to 623; and it's fast, very fast. 
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Frontier {
	class Random {
		private const int
			M	               = 397,
			N                = 624;

		private const uint
			LOWER_MASK       = 0x7fffffff,
			MATRIX_A				 = 0x9908b0df,
			TEMPERING_MASK_B = 0x9d2c5680,
			TEMPERING_MASK_C = 0xefc60000,
			UPPER_MASK       = 0x80000000;

		private static int   k = 1;
		private static uint[] mag01 = { 0x0, MATRIX_A };
		private static int[] ptgfsr = new int[N];

		private static int TEMPERING_SHIFT_L(int y) { return (y >> 18); }
		private static int TEMPERING_SHIFT_S(int y) { return (y << 7); }
		private static int TEMPERING_SHIFT_T(int y) { return (y << 15); }
		private static int TEMPERING_SHIFT_U(int y) { return (y >> 11); }


		public static int Value() {
			int		kk,	y;

			if (k == N) {
				for (kk = 0; kk < N - M; kk++) {
					y = (ptgfsr[kk] & UPPER_MASK) | (ptgfsr[kk + 1] & LOWER_MASK);
					ptgfsr[kk] = ptgfsr[kk + M] ^ (y >> 1) ^ mag01[y & 0x1];
				}
				for (; kk < N - 1; kk++) {
					y = (ptgfsr[kk] & UPPER_MASK) | (ptgfsr[kk + 1] & LOWER_MASK);
					ptgfsr[kk] = ptgfsr[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1];
				}
				y = (ptgfsr[N - 1] & UPPER_MASK) | (ptgfsr[0] & LOWER_MASK);
				ptgfsr[N - 1] = ptgfsr[M - 1] ^ (y >> 1) ^ mag01[y & 0x1];
				k = 0;
			}
			y = ptgfsr[k++];
			y ^= TEMPERING_SHIFT_U(y);
			y ^= TEMPERING_SHIFT_S(y) & TEMPERING_MASK_B;
			y ^= TEMPERING_SHIFT_T(y) & TEMPERING_MASK_C;
			return y ^= TEMPERING_SHIFT_L(y);
		}

		public static int Value(int range) {
			return (range != 0) ? (Value() % range) : 0;
		}

		public static float Float() {
			return (float) Value(10000) / 10000;
		}

		public static void Init(int seed) {
			mag01[0] = 0;
			mag01[1] = MATRIX_A;
			ptgfsr[0] = seed;
			for (k = 1; k < N; k++)
				ptgfsr[k] = 69069 * ptgfsr[k - 1];
			k = 1;
		}

		public static bool CoinFlip() {
			return (Value(2) == 0);
		}
	}
}