namespace FrontierSharp.Texture {
    using System;
    using System.Drawing;

    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Util;
    using System.Drawing.Imaging;

    /// <summary>This loads in textures.Nothin' fancy.</summary>
    public class TexturesImpl : ITextures {
        private const int max_STRING = 128;

        // TODO: Textures are currently stored as a homemade linked list. Evaluate whether to change to
        // a generic linked list implementation, or store them in a hash map
        private Texture headTexture;

        public uint TextureIdFromName(string name) {
            Texture t = TextureFromName(name);
            if (null != t)
                return t.id;
            return 0;
        }

        public Texture TextureFromName(string name) {
            Texture t;

            for (t = headTexture; null != t; t = t.next) {
                if (string.Equals(name, t.name, StringComparison.OrdinalIgnoreCase)) {
                    return t;
                }
            }
            t = LoadTexture(name);
            return t;
        }

        private Texture LoadTexture(string name) {
            uint id;
            GL.GenTextures(1, out id);

            GL.BindTexture(TextureTarget.Texture2D, id);

            string filename = string.Format("textures/{0}", name);
            Coord size;
            Bitmap bitmap = FileUtils.FileImageLoad(filename, out size);
            BitmapData data = null;

            try {
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Four, size.X, size.Y, 0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
            } finally {
                bitmap.UnlockBits(data);
            }
            // TODO: check if this does what it's supposed to do (line below is original)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            //gluBuild2DMipmaps(GL_TEXTURE_2D, 4, size.x, size.y, GL_RGBA, GL_UNSIGNED_BYTE, buffer);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            var t = new Texture() {
                name = name,
                id = id,
                next = headTexture,
                width = size.X,
                height = size.Y
            };
            headTexture = t;

            return t;
        }

        private void TexturePurge() {
            while (null != headTexture) {
                Texture t = headTexture;
                GL.DeleteTextures(1, new uint[] { t.id });
                headTexture = t.next;
            }
        }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }
    }
}
