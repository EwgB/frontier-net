namespace FrontierSharp.Textures {
    using System;
    using System.Drawing;

    using OpenTK.Graphics.OpenGL;

    using Common.Textures;
    using Common.Util;
    using System.Drawing.Imaging;

    /// <summary>This loads in textures.Nothin' fancy.</summary>
    internal class TexturesImpl : ITextures {

        // TODO: Textures are currently stored as a homemade linked list. Evaluate whether to change to
        // a generic linked list implementation, or store them in a hash map
        private Texture headTexture;

        public uint TextureIdFromName(string name) {
            var t = TextureFromName(name);
            return t?.Id ?? 0;
        }

        private Texture TextureFromName(string name) {
            for (var t = this.headTexture; null != t; t = t.Next) {
                if (string.Equals(name, t.Name, StringComparison.OrdinalIgnoreCase)) {
                    return t;
                }
            }
            return LoadTexture(name);
        }

        private Texture LoadTexture(string name) {
            uint id;
            GL.GenTextures(1, out id);

            GL.BindTexture(TextureTarget.Texture2D, id);

            var filename = $"textures/{name}";
            Coord size;
            var bitmap = FileUtils.FileImageLoad(filename, out size);
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

            var t = new Texture {
                Name = name,
                Id = id,
                Next = this.headTexture,
                Width = size.X,
                Height = size.Y
            };
            this.headTexture = t;

            return t;
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }

        #region Dispose pattern

        private void ReleaseUnmanagedResources() {
            while (null != this.headTexture) {
                var t = this.headTexture;
                GL.DeleteTextures(1, new[] { t.Id });
                this.headTexture = t.Next;
            }
        }

        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~TexturesImpl() {
            ReleaseUnmanagedResources();
        }

        #endregion
    }
}
