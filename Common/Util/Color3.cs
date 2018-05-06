namespace FrontierSharp.Common.Util {
    using System;
    using System.Drawing;

    using OpenTK;

    /// <summary>
    /// Represents a color with 3 floating-point components (R, G, B).
    /// Based on OpenTK.Graphics.Color4
    /// </summary>
    [Serializable]
    public struct Color3 : IEquatable<Color3> {
        #region Fields

        /// <summary>
        /// The R component of this Color3 structure.
        /// </summary>
        public float R { get; }

        /// <summary>
        /// The G component of this Color3 structure.
        /// </summary>
        public float G { get; }

        /// <summary>
        /// The B component of this Color3 structure.
        /// </summary>
        public float B { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new Color3 structure from the specified components.
        /// </summary>
        /// <param name="r">The R component of the new Color3 structure.</param>
        /// <param name="g">The G component of the new Color3 structure.</param>
        /// <param name="b">The B component of the new Color3 structure.</param>
        public Color3(float r, float g, float b) {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        /// <summary>
        /// Constructs a new Color3 structure from the specified components.
        /// </summary>
        /// <param name="r">The R component of the new Color3 structure.</param>
        /// <param name="g">The G component of the new Color3 structure.</param>
        /// <param name="b">The B component of the new Color3 structure.</param>
        public Color3(byte r, byte g, byte b)
            : this(
                  r / (float)byte.MaxValue,
                  g / (float)byte.MaxValue,
                  b / (float)byte.MaxValue) { }

        /// <summary>
        /// Constructs a new Color3 structure from the specified System.Drawing.Color.
        /// </summary>
        /// <param name="color">The System.Drawing.Color containing the component values.</param>
        public Color3(Color color) : this(color.R, color.G, color.B) { }

        #endregion

        #region Public Members

        /// <summary>
        /// Converts this color to an integer representation with 8 bits per channel.
        /// </summary>
        /// <returns>A <see cref="System.Int32"/> that represents this instance.</returns>
        /// <remarks>This method is intended only for compatibility with System.Drawing. It compresses the color
        /// into 8 bits per channel, which means color information is lost.</remarks>
        public int ToArgb() {
            uint value =
                (uint)(byte.MaxValue) << 24 |       // Alpha is always 1
                (uint)(this.R * byte.MaxValue) << 16 |
                (uint)(this.G * byte.MaxValue) << 8 |
                (uint)(this.B * byte.MaxValue);

            return unchecked((int)value);
        }

        /// <summary>
        /// Compares whether this Color3 structure is equal to the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to.</param>
        /// <returns>True obj is a Color3 structure with the same components as this Color3; false otherwise.</returns>
        public override bool Equals(object obj) {
            if (!(obj is Color3))
                return false;

            return Equals((Color3)obj);
        }

        /// <summary>
        /// Calculates the hash code for this Color3 structure.
        /// </summary>
        /// <returns>A System.Int32 containing the hashcode of this Color3 structure.</returns>
        public override int GetHashCode() {
            return ToArgb();
        }

        /// <summary>
        /// Creates a System.String that describes this Color3 structure.
        /// </summary>
        /// <returns>A System.String that describes this Color3 structure.</returns>
        public override string ToString() {
            return $"{{(R, G, B) = ({this.R}, {this.G}, {this.B})}}";
        }

        /// <summary>
        /// Scales all values down to range between 0 and 1
        /// </summary>
        /// <returns>A new Color3 with normalized RGB values</returns>
        public Color3 Normalize() {
            return this / Math.Min(Math.Max(this.R, Math.Max(this.G, this.B)), 1);
        }

        public Color3 Clamp() {
            return new Color3(
                MathHelper.Clamp(this.R, 0, 1),
                MathHelper.Clamp(this.G, 0, 1),
                MathHelper.Clamp(this.B, 0, 1));
        }

        #region Operators

        #region Comparison

        /// <summary>
        /// Compares the specified Color3 structures for equality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is equal to right; false otherwise.</returns>
        public static bool operator ==(Color3 left, Color3 right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified Color3 structures for inequality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is not equal to right; false otherwise.</returns>
        public static bool operator !=(Color3 left, Color3 right) {
            return !left.Equals(right);
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Converts the specified System.Drawing.Color to a Color3 structure.
        /// </summary>
        /// <param name="color">The System.Drawing.Color to convert.</param>
        /// <returns>A new Color3 structure containing the converted components.</returns>
        public static implicit operator Color3(Color color) {
            return new Color3(color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts the specified Color3 to a System.Drawing.Color structure.
        /// </summary>
        /// <param name="color">The Color3 to convert.</param>
        /// <returns>A new System.Drawing.Color structure containing the converted components.</returns>
        public static explicit operator Color(Color3 color) {
            return Color.FromArgb(
                (int)(color.R * byte.MaxValue),
                (int)(color.G * byte.MaxValue),
                (int)(color.B * byte.MaxValue));
        }

        #endregion

        #region Addition

        public static Color3 operator +(Color3 left, Color3 right) {
            return new Color3(left.R + right.R, left.G + right.G, left.B + right.B);
        }

        public static Color3 operator +(Color3 c, float x) {
            return new Color3(c.R + x, c.G + x, c.B + x);
        }

        #endregion

        #region Substraction

        public static Color3 operator -(Color3 left, Color3 right) {
            return new Color3(left.R - right.R, left.G - right.G, left.B - right.B);
        }

        public static Color3 operator -(Color3 c, float x) {
            return new Color3(c.R - x, c.G - x, c.B - x);
        }

        #endregion

        #region Multiplication

        public static Color3 operator *(Color3 left, Color3 right) {
            return new Color3(left.R * right.R, left.G * right.G, left.B * right.B);
        }

        public static Color3 operator *(Color3 c, float x) {
            return new Color3(c.R * x, c.G * x, c.B * x);
        }

        #endregion

        #region Division

        public static Color3 operator /(Color3 left, Color3 right) {
            return new Color3(left.R / right.R, left.G / right.G, left.B / right.B);
        }

        public static Color3 operator /(Color3 c, float x) {
            return new Color3(c.R / x, c.G / x, c.B / x);
        }

        #endregion

        #endregion

        #region System colors

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255).
        /// </summary>
        public static Color3 AliceBlue => new Color3(240, 248, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215).
        /// </summary>
        public static Color3 AntiqueWhite => new Color3(250, 235, 215);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255).
        /// </summary>
        public static Color3 Aqua => new Color3(0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212).
        /// </summary>
        public static Color3 Aquamarine => new Color3(127, 255, 212);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255).
        /// </summary>
        public static Color3 Azure => new Color3(240, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220).
        /// </summary>
        public static Color3 Beige => new Color3(245, 245, 220);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196).
        /// </summary>
        public static Color3 Bisque => new Color3(255, 228, 196);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0).
        /// </summary>
        public static Color3 Black => new Color3(0, 0, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205).
        /// </summary>
        public static Color3 BlanchedAlmond => new Color3(255, 235, 205);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255).
        /// </summary>
        public static Color3 Blue => new Color3(0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226).
        /// </summary>
        public static Color3 BlueViolet => new Color3(138, 43, 226);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42).
        /// </summary>
        public static Color3 Brown => new Color3(165, 42, 42);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135).
        /// </summary>
        public static Color3 BurlyWood => new Color3(222, 184, 135);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160).
        /// </summary>
        public static Color3 CadetBlue => new Color3(95, 158, 160);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0).
        /// </summary>
        public static Color3 Chartreuse => new Color3(127, 255, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30).
        /// </summary>
        public static Color3 Chocolate => new Color3(210, 105, 30);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80).
        /// </summary>
        public static Color3 Coral => new Color3(255, 127, 80);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237).
        /// </summary>
        public static Color3 CornflowerBlue => new Color3(100, 149, 237);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220).
        /// </summary>
        public static Color3 Cornsilk => new Color3(255, 248, 220);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60).
        /// </summary>
        public static Color3 Crimson => new Color3(220, 20, 60);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255).
        /// </summary>
        public static Color3 Cyan => new Color3(0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139).
        /// </summary>
        public static Color3 DarkBlue => new Color3(0, 0, 139);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139).
        /// </summary>
        public static Color3 DarkCyan => new Color3(0, 139, 139);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11).
        /// </summary>
        public static Color3 DarkGoldenrod => new Color3(184, 134, 11);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169).
        /// </summary>
        public static Color3 DarkGray => new Color3(169, 169, 169);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0).
        /// </summary>
        public static Color3 DarkGreen => new Color3(0, 100, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107).
        /// </summary>
        public static Color3 DarkKhaki => new Color3(189, 183, 107);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139).
        /// </summary>
        public static Color3 DarkMagenta => new Color3(139, 0, 139);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47).
        /// </summary>
        public static Color3 DarkOliveGreen => new Color3(85, 107, 47);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0).
        /// </summary>
        public static Color3 DarkOrange => new Color3(255, 140, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204).
        /// </summary>
        public static Color3 DarkOrchid => new Color3(153, 50, 204);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0).
        /// </summary>
        public static Color3 DarkRed => new Color3(139, 0, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122).
        /// </summary>
        public static Color3 DarkSalmon => new Color3(233, 150, 122);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139).
        /// </summary>
        public static Color3 DarkSeaGreen => new Color3(143, 188, 139);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139).
        /// </summary>
        public static Color3 DarkSlateBlue => new Color3(72, 61, 139);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79).
        /// </summary>
        public static Color3 DarkSlateGray => new Color3(47, 79, 79);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209).
        /// </summary>
        public static Color3 DarkTurquoise => new Color3(0, 206, 209);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211).
        /// </summary>
        public static Color3 DarkViolet => new Color3(148, 0, 211);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147).
        /// </summary>
        public static Color3 DeepPink => new Color3(255, 20, 147);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255).
        /// </summary>
        public static Color3 DeepSkyBlue => new Color3(0, 191, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105).
        /// </summary>
        public static Color3 DimGray => new Color3(105, 105, 105);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255).
        /// </summary>
        public static Color3 DodgerBlue => new Color3(30, 144, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34).
        /// </summary>
        public static Color3 Firebrick => new Color3(178, 34, 34);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240).
        /// </summary>
        public static Color3 FloralWhite => new Color3(255, 250, 240);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34).
        /// </summary>
        public static Color3 ForestGreen => new Color3(34, 139, 34);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255).
        /// </summary>
        public static Color3 Fuchsia => new Color3(255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220).
        /// </summary>
        public static Color3 Gainsboro => new Color3(220, 220, 220);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255).
        /// </summary>
        public static Color3 GhostWhite => new Color3(248, 248, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0).
        /// </summary>
        public static Color3 Gold => new Color3(255, 215, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32).
        /// </summary>
        public static Color3 Goldenrod => new Color3(218, 165, 32);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128).
        /// </summary>
        public static Color3 Gray => new Color3(128, 128, 128);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0).
        /// </summary>
        public static Color3 Green => new Color3(0, 128, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47).
        /// </summary>
        public static Color3 GreenYellow => new Color3(173, 255, 47);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240).
        /// </summary>
        public static Color3 Honeydew => new Color3(240, 255, 240);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180).
        /// </summary>
        public static Color3 HotPink => new Color3(255, 105, 180);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92).
        /// </summary>
        public static Color3 IndianRed => new Color3(205, 92, 92);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130).
        /// </summary>
        public static Color3 Indigo => new Color3(75, 0, 130);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240).
        /// </summary>
        public static Color3 Ivory => new Color3(255, 255, 240);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140).
        /// </summary>
        public static Color3 Khaki => new Color3(240, 230, 140);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250).
        /// </summary>
        public static Color3 Lavender => new Color3(230, 230, 250);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245).
        /// </summary>
        public static Color3 LavenderBlush => new Color3(255, 240, 245);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0).
        /// </summary>
        public static Color3 LawnGreen => new Color3(124, 252, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205).
        /// </summary>
        public static Color3 LemonChiffon => new Color3(255, 250, 205);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230).
        /// </summary>
        public static Color3 LightBlue => new Color3(173, 216, 230);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128).
        /// </summary>
        public static Color3 LightCoral => new Color3(240, 128, 128);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255).
        /// </summary>
        public static Color3 LightCyan => new Color3(224, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210).
        /// </summary>
        public static Color3 LightGoldenrodYellow => new Color3(250, 250, 210);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144).
        /// </summary>
        public static Color3 LightGreen => new Color3(144, 238, 144);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211).
        /// </summary>
        public static Color3 LightGray => new Color3(211, 211, 211);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193).
        /// </summary>
        public static Color3 LightPink => new Color3(255, 182, 193);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122).
        /// </summary>
        public static Color3 LightSalmon => new Color3(255, 160, 122);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170).
        /// </summary>
        public static Color3 LightSeaGreen => new Color3(32, 178, 170);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250).
        /// </summary>
        public static Color3 LightSkyBlue => new Color3(135, 206, 250);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153).
        /// </summary>
        public static Color3 LightSlateGray => new Color3(119, 136, 153);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222).
        /// </summary>
        public static Color3 LightSteelBlue => new Color3(176, 196, 222);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224).
        /// </summary>
        public static Color3 LightYellow => new Color3(255, 255, 224);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0).
        /// </summary>
        public static Color3 Lime => new Color3(0, 255, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50).
        /// </summary>
        public static Color3 LimeGreen => new Color3(50, 205, 50);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230).
        /// </summary>
        public static Color3 Linen => new Color3(250, 240, 230);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255).
        /// </summary>
        public static Color3 Magenta => new Color3(255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0).
        /// </summary>
        public static Color3 Maroon => new Color3(128, 0, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170).
        /// </summary>
        public static Color3 MediumAquamarine => new Color3(102, 205, 170);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205).
        /// </summary>
        public static Color3 MediumBlue => new Color3(0, 0, 205);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211).
        /// </summary>
        public static Color3 MediumOrchid => new Color3(186, 85, 211);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219).
        /// </summary>
        public static Color3 MediumPurple => new Color3(147, 112, 219);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113).
        /// </summary>
        public static Color3 MediumSeaGreen => new Color3(60, 179, 113);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238).
        /// </summary>
        public static Color3 MediumSlateBlue => new Color3(123, 104, 238);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154).
        /// </summary>
        public static Color3 MediumSpringGreen => new Color3(0, 250, 154);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204).
        /// </summary>
        public static Color3 MediumTurquoise => new Color3(72, 209, 204);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133).
        /// </summary>
        public static Color3 MediumVioletRed => new Color3(199, 21, 133);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112).
        /// </summary>
        public static Color3 MidnightBlue => new Color3(25, 25, 112);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250).
        /// </summary>
        public static Color3 MintCream => new Color3(245, 255, 250);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225).
        /// </summary>
        public static Color3 MistyRose => new Color3(255, 228, 225);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181).
        /// </summary>
        public static Color3 Moccasin => new Color3(255, 228, 181);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173).
        /// </summary>
        public static Color3 NavajoWhite => new Color3(255, 222, 173);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128).
        /// </summary>
        public static Color3 Navy => new Color3(0, 0, 128);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230).
        /// </summary>
        public static Color3 OldLace => new Color3(253, 245, 230);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0).
        /// </summary>
        public static Color3 Olive => new Color3(128, 128, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35).
        /// </summary>
        public static Color3 OliveDrab => new Color3(107, 142, 35);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0).
        /// </summary>
        public static Color3 Orange => new Color3(255, 165, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0).
        /// </summary>
        public static Color3 OrangeRed => new Color3(255, 69, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214).
        /// </summary>
        public static Color3 Orchid => new Color3(218, 112, 214);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170).
        /// </summary>
        public static Color3 PaleGoldenrod => new Color3(238, 232, 170);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152).
        /// </summary>
        public static Color3 PaleGreen => new Color3(152, 251, 152);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238).
        /// </summary>
        public static Color3 PaleTurquoise => new Color3(175, 238, 238);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147).
        /// </summary>
        public static Color3 PaleVioletRed => new Color3(219, 112, 147);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213).
        /// </summary>
        public static Color3 PapayaWhip => new Color3(255, 239, 213);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185).
        /// </summary>
        public static Color3 PeachPuff => new Color3(255, 218, 185);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63).
        /// </summary>
        public static Color3 Peru => new Color3(205, 133, 63);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203).
        /// </summary>
        public static Color3 Pink => new Color3(255, 192, 203);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221).
        /// </summary>
        public static Color3 Plum => new Color3(221, 160, 221);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230).
        /// </summary>
        public static Color3 PowderBlue => new Color3(176, 224, 230);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128).
        /// </summary>
        public static Color3 Purple => new Color3(128, 0, 128);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0).
        /// </summary>
        public static Color3 Red => new Color3(255, 0, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143).
        /// </summary>
        public static Color3 RosyBrown => new Color3(188, 143, 143);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225).
        /// </summary>
        public static Color3 RoyalBlue => new Color3(65, 105, 225);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19).
        /// </summary>
        public static Color3 SaddleBrown => new Color3(139, 69, 19);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114).
        /// </summary>
        public static Color3 Salmon => new Color3(250, 128, 114);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96).
        /// </summary>
        public static Color3 SandyBrown => new Color3(244, 164, 96);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87).
        /// </summary>
        public static Color3 SeaGreen => new Color3(46, 139, 87);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238).
        /// </summary>
        public static Color3 SeaShell => new Color3(255, 245, 238);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45).
        /// </summary>
        public static Color3 Sienna => new Color3(160, 82, 45);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192).
        /// </summary>
        public static Color3 Silver => new Color3(192, 192, 192);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235).
        /// </summary>
        public static Color3 SkyBlue => new Color3(135, 206, 235);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205).
        /// </summary>
        public static Color3 SlateBlue => new Color3(106, 90, 205);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144).
        /// </summary>
        public static Color3 SlateGray => new Color3(112, 128, 144);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250).
        /// </summary>
        public static Color3 Snow => new Color3(255, 250, 250);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127).
        /// </summary>
        public static Color3 SpringGreen => new Color3(0, 255, 127);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180).
        /// </summary>
        public static Color3 SteelBlue => new Color3(70, 130, 180);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140).
        /// </summary>
        public static Color3 Tan => new Color3(210, 180, 140);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128).
        /// </summary>
        public static Color3 Teal => new Color3(0, 128, 128);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216).
        /// </summary>
        public static Color3 Thistle => new Color3(216, 191, 216);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71).
        /// </summary>
        public static Color3 Tomato => new Color3(255, 99, 71);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208).
        /// </summary>
        public static Color3 Turquoise => new Color3(64, 224, 208);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238).
        /// </summary>
        public static Color3 Violet => new Color3(238, 130, 238);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179).
        /// </summary>
        public static Color3 Wheat => new Color3(245, 222, 179);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255).
        /// </summary>
        public static Color3 White => new Color3(255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245).
        /// </summary>
        public static Color3 WhiteSmoke => new Color3(245, 245, 245);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0).
        /// </summary>
        public static Color3 Yellow => new Color3(255, 255, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50).
        /// </summary>
        public static Color3 YellowGreen => new Color3(154, 205, 50);

        #endregion

        #endregion

        #region Color conversions

        #region sRGB

        /// <summary>
        /// Converts sRGB color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="srgb">
        /// Color value to convert in sRGB.
        /// </param>
        public static Color3 FromSrgb(Color3 srgb) {
            float r, g, b;

            if (srgb.R <= 0.04045f) {
                r = srgb.R / 12.92f;
            } else {
                r = (float)Math.Pow((srgb.R + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            if (srgb.G <= 0.04045f) {
                g = srgb.G / 12.92f;
            } else {
                g = (float)Math.Pow((srgb.G + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            if (srgb.B <= 0.04045f) {
                b = srgb.B / 12.92f;
            } else {
                b = (float)Math.Pow((srgb.B + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            return new Color3(r, g, b);
        }

        /// <summary>
        /// Converts RGB color values to sRGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Color3 ToSrgb(Color3 rgb) {
            float r, g, b;

            if (rgb.R <= 0.0031308) {
                r = 12.92f * rgb.R;
            } else {
                r = (1.0f + 0.055f) * (float)Math.Pow(rgb.R, 1.0f / 2.4f) - 0.055f;
            }

            if (rgb.G <= 0.0031308) {
                g = 12.92f * rgb.G;
            } else {
                g = (1.0f + 0.055f) * (float)Math.Pow(rgb.G, 1.0f / 2.4f) - 0.055f;
            }

            if (rgb.B <= 0.0031308) {
                b = 12.92f * rgb.B;
            } else {
                b = (1.0f + 0.055f) * (float)Math.Pow(rgb.B, 1.0f / 2.4f) - 0.055f;
            }

            return new Color3(r, g, b);
        }

        #endregion

        #region HSL

        /// <summary>
        /// Converts HSL color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hsl">
        /// Color value to convert in hue, saturation, lightness (HSL).
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L),
        /// and the W element is Alpha (which is ignoR).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color3 FromHsl(Vector4 hsl) {
            var hue = hsl.X * 360.0f;
            var saturation = hsl.Y;
            var lightness = hsl.Z;

            var c = (1.0f - Math.Abs(2.0f * lightness - 1.0f)) * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f) {
                r = c;
                g = x;
                b = 0.0f;
            } else if (1.0f <= h && h < 2.0f) {
                r = x;
                g = c;
                b = 0.0f;
            } else if (2.0f <= h && h < 3.0f) {
                r = 0.0f;
                g = c;
                b = x;
            } else if (3.0f <= h && h < 4.0f) {
                r = 0.0f;
                g = x;
                b = c;
            } else if (4.0f <= h && h < 5.0f) {
                r = x;
                g = 0.0f;
                b = c;
            } else if (5.0f <= h && h < 6.0f) {
                r = c;
                g = 0.0f;
                b = x;
            } else {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = lightness - (c / 2.0f);
            return new Color3(r + m, g + m, b + m);
        }

        /// <summary>
        /// Converts RGB color values to HSL color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L),
        /// and the W element is Alpha (which is set to 1).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHsl(Color3 rgb) {
            var max = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var min = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var c = max - min;

            float h = 0.0f;
            if (max == rgb.R) {
                h = ((rgb.G - rgb.B) / c);
            } else if (max == rgb.G) {
                h = ((rgb.B - rgb.R) / c) + 2.0f;
            } else if (max == rgb.B) {
                h = ((rgb.R - rgb.G) / c) + 4.0f;
            }

            var hue = h / 6.0f;
            if (hue < 0.0f) {
                hue += 1.0f;
            }

            var lightness = (max + min) / 2.0f;

            var saturation = 0.0f;
            if (0.0f != lightness && lightness != 1.0f) {
                saturation = c / (1.0f - Math.Abs(2.0f * lightness - 1.0f));
            }

            return new Vector4(hue, saturation, lightness, 1);
        }

        #endregion

        #region HSV

        /// <summary>
        /// Converts HSV color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hsv">
        /// Color value to convert in hue, saturation, value (HSV).
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V),
        /// and the W element is Alpha (which is ignoR).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color3 FromHsv(Vector4 hsv) {
            var hue = hsv.X * 360.0f;
            var saturation = hsv.Y;
            var value = hsv.Z;

            var c = value * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f) {
                r = c;
                g = x;
                b = 0.0f;
            } else if (1.0f <= h && h < 2.0f) {
                r = x;
                g = c;
                b = 0.0f;
            } else if (2.0f <= h && h < 3.0f) {
                r = 0.0f;
                g = c;
                b = x;
            } else if (3.0f <= h && h < 4.0f) {
                r = 0.0f;
                g = x;
                b = c;
            } else if (4.0f <= h && h < 5.0f) {
                r = x;
                g = 0.0f;
                b = c;
            } else if (5.0f <= h && h < 6.0f) {
                r = c;
                g = 0.0f;
                b = x;
            } else {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = value - c;
            return new Color3(r + m, g + m, b + m);
        }

        /// <summary>
        /// Converts RGB color values to HSV color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V),
        /// and the W element is Alpha (which is set to 1).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHsv(Color3 rgb) {
            var max = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var min = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var c = max - min;

            float h = 0.0f;
            if (max == rgb.R) {
                h = ((rgb.G - rgb.B) / c) % 6.0f;
            } else if (max == rgb.G) {
                h = ((rgb.B - rgb.R) / c) + 2.0f;
            } else if (max == rgb.B) {
                h = ((rgb.R - rgb.G) / c) + 4.0f;
            }

            var hue = (h * 60.0f) / 360.0f;

            var saturation = 0.0f;
            if (0.0f != max) {
                saturation = c / max;
            }

            return new Vector4(hue, saturation, max, 1);
        }

        #endregion

        #region XYZ

        /// <summary>
        /// Converts XYZ color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="xyz">
        /// Color value to convert with the trisimulus values of X, Y, and Z in the corresponding element,
        /// and the W element with Alpha (which is ignoR).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Color3 FromXyz(Vector4 xyz) {
            var r = 0.41847f * xyz.X + -0.15866f * xyz.Y + -0.082835f * xyz.Z;
            var g = -0.091169f * xyz.X + 0.25243f * xyz.Y + 0.015708f * xyz.Z;
            var b = 0.00092090f * xyz.X + -0.0025498f * xyz.Y + 0.17860f * xyz.Z;
            return new Color3(r, g, b);
        }

        /// <summary>
        /// Converts RGB color values to XYZ color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value with the trisimulus values of X, Y, and Z in the corresponding element,
        /// and the W element with Alpha (which is set to 1).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Vector4 ToXyz(Color3 rgb) {
            var x = (0.49f * rgb.R + 0.31f * rgb.G + 0.20f * rgb.B) / 0.17697f;
            var y = (0.17697f * rgb.R + 0.81240f * rgb.G + 0.01063f * rgb.B) / 0.17697f;
            var z = (0.00f * rgb.R + 0.01f * rgb.G + 0.99f * rgb.B) / 0.17697f;
            return new Vector4(x, y, z, 1);
        }

        #endregion

        #region YUV

        /// <summary>
        /// Converts YCbCr color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="ycbcr">
        /// Color value to convert in Luma-Chrominance (YCbCr) aka YUV.
        /// The X element contains Luma (Y, 0.0 to 1.0), the Y element contains B-difference chroma (U, -0.5 to 0.5),
        /// the Z element contains the R-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (which is ignoR).
        /// </param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Color3 FromYcbcr(Vector4 ycbcr) {
            var r = 1.0f * ycbcr.X + 0.0f * ycbcr.Y + 1.402f * ycbcr.Z;
            var g = 1.0f * ycbcr.X + -0.344136f * ycbcr.Y + -0.714136f * ycbcr.Z;
            var b = 1.0f * ycbcr.X + 1.772f * ycbcr.Y + 0.0f * ycbcr.Z;
            return new Color3(r, g, b);
        }

        /// <summary>
        /// Converts RGB color values to YUV color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value in Luma-Chrominance (YCbCr) aka YUV.
        /// The X element contains Luma (Y, 0.0 to 1.0), the Y element contains B-difference chroma (U, -0.5 to 0.5),
        /// the Z element contains the R-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (which is set to 1).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Vector4 ToYcbcr(Color3 rgb) {
            var y = 0.299f * rgb.R + 0.587f * rgb.G + 0.114f * rgb.B;
            var u = -0.168736f * rgb.R + -0.331264f * rgb.G + 0.5f * rgb.B;
            var v = 0.5f * rgb.R + -0.418688f * rgb.G + -0.081312f * rgb.B;
            return new Vector4(y, u, v, 1);
        }

        #endregion

        #region HCY

        /// <summary>
        /// Converts HCY color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hcy">
        /// Color value to convert in hue, chroma, luminance (HCY).
        /// The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y),
        /// and the W element is Alpha (which is ignoR).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color3 FromHcy(Vector4 hcy) {
            var hue = hcy.X * 360.0f;
            var c = hcy.Y;
            var luminance = hcy.Z;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f) {
                r = c;
                g = x;
                b = 0.0f;
            } else if (1.0f <= h && h < 2.0f) {
                r = x;
                g = c;
                b = 0.0f;
            } else if (2.0f <= h && h < 3.0f) {
                r = 0.0f;
                g = c;
                b = x;
            } else if (3.0f <= h && h < 4.0f) {
                r = 0.0f;
                g = x;
                b = c;
            } else if (4.0f <= h && h < 5.0f) {
                r = x;
                g = 0.0f;
                b = c;
            } else if (5.0f <= h && h < 6.0f) {
                r = c;
                g = 0.0f;
                b = x;
            } else {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = luminance - (0.30f * r + 0.59f * g + 0.11f * b);
            return new Color3(r + m, g + m, b + m);
        }

        /// <summary>
        /// Converts RGB color values to HCY color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y),
        /// and the W element is Alpha (which is set to 1).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHcy(Color3 rgb) {
            var max = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var min = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var c = max - min;

            var h = 0.0f;
            if (max == rgb.R) {
                h = ((rgb.G - rgb.B) / c) % 6.0f;
            } else if (max == rgb.G) {
                h = ((rgb.B - rgb.R) / c) + 2.0f;
            } else if (max == rgb.B) {
                h = ((rgb.R - rgb.G) / c) + 4.0f;
            }

            var hue = (h * 60.0f) / 360.0f;

            var luminance = 0.30f * rgb.R + 0.59f * rgb.G + 0.11f * rgb.B;

            return new Vector4(hue, c, luminance, 1);
        }

        #endregion

        #endregion

        #region IEquatable<Color3> Members

        /// <summary>
        /// Compares whether this Color3 structure is equal to the specified Color3.
        /// </summary>
        /// <param name="other">The Color3 structure to compare to.</param>
        /// <returns>True if both Color3 structures contain the same components; false otherwise.</returns>
        public bool Equals(Color3 other) {
            return this.R == other.R && this.G == other.G && this.B == other.B;
        }

        #endregion
    }
}

/* From glRgba.cpp
GLrgba glRgba(char* string)
{

long color;
char buffer[10];
char* pound;
GLrgba result;

strncmp(buffer, string, 10);
if (pound = strchr(buffer, '#'))
pound[0] = ' ';
if (sscanf(string, "%x", &color) != 1)
return glRgba(0.0f);
result.R = (float)GetBValue(color) / 255.0f;
result.G = (float)GetGValue(color) / 255.0f;
result.B = (float)GetRValue(color) / 255.0f;
result.A = 1.0f;
return result;

}


}


float Color3::Brighness() {

return (R + B + G) / 3.0f;

}
*/
