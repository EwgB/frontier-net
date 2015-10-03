namespace terrain_sharp.Source.StdAfx {
	class StdAfx {
		//#define WRAP(x,y)                 ((unsigned)x % y)
		//#define SIGN(x)                   (((x) > 0) ? 1 : ((x) < 0) ? -1 : 0)
		//#define SIGNF(x)                  (((x) > NEGLIGIBLE) ? 1 : ((x) < -NEGLIGIBLE) ? -1 : 0)
		//#define ABS(x)                    (((x) < 0 ? (-x) : (x)))
		//#define SMALLEST(x,y)             (ABS(x) < ABS(y) ? 0 : x)                
		//#define SWAP(a,b)                 {int temp = a;a = b; b = temp;}
		//#define SWAPF(a,b)                {float temp = a;a = b; b = temp;}
		//#define ARGS(text, args)          { va_list		ap;	text[0] = 0; if (args != NULL)	{ va_start(ap, args); vsprintf(text, args, ap); va_end(ap);}	}
		//#define INTERPOLATE(a,b,delta)    (a * (1.0f - delta) + b * delta)
		//#define clamp(n,lower,upper)      (max (min(n,(upper)), (lower)))

		public const float FREEZING = 0.32f;
		public const float TEMP_COLD = 0.45f;
		public const float TEMP_TEMPERATE = 0.6f;
		public const float TEMP_HOT = 0.9f;
    public const float MIN_TEMP = 0;
		public const float MAX_TEMP = 1;
		//#define NEGLIGIBLE                0.000000000001f
		//#define GRAVITY                   9.5f

		//This is used to scale the z value of normals
		//Nower numbers make the normals more extreme, exaggerate the lighting
		public const float NORMAL_SCALING = 0.6f;
	}
}
