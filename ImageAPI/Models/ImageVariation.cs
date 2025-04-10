namespace ImageAPI.Models
{
    /// <summary>
    /// Represents a variation of an image, typically based on size (height, width) adjustments.
    /// </summary>
    public class ImageVariation
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string Path { get; set; }

        public ImageVariation(int height, int width, string path)
        {
            Height = height;
            Width = width;
            Path = path;
        }
    }
}
