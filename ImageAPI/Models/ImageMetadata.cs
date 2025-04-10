namespace ImageAPI.Models
{
    /// <summary>
    /// Contains metadata information for a specific image, including the original image and its variations.
    /// </summary>
    public class ImageMetadata
    {
        public Guid Id { get; set; }
        public ImageVariation Original { get; set; }
        public List<ImageVariation> Variations { get; set; }
        public DateTime UploadDate { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }

        public ImageMetadata()
        {
            Variations = new List<ImageVariation>();
        }

        public ImageMetadata(Guid id) 
        {
            Id = id;
            Variations = new List<ImageVariation>();
        }

        public ImageMetadata(Guid id, DateTime uploadDate, long fileSize, string fileType)
        {
            Id = id;
            UploadDate = uploadDate;
            FileSize = fileSize;
            FileType = fileType;
            Variations = new List<ImageVariation>();
        }
    }
}
