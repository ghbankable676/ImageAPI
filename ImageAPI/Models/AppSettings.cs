namespace ImageAPI.Models
{
    /// <summary>
    /// Represents the configuration settings for the application, obtained from appsettings.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Base path where images are stored
        /// </summary>
        public string ImageBasePath { get; set; }
        public bool UseMongo { get; set; }
    }
}