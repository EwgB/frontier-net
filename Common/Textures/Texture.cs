namespace FrontierSharp.Common.Textures {
    public class Texture {
        public Texture next;
        public uint id;
        public string name;
        string image_name;
        public int width;
        public int height;
        short bpp;//bytes per pixel
    }
}