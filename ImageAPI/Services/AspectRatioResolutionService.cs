using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


namespace ImageAPI.Services
{
    /// <summary>
    /// Service for resolving and loading aspect ratio configurations from a file.
    /// </summary>
    public class AspectRatioResolutionService
    {
        public class Resolution
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class AspectRatioData
        {
            public Dictionary<string, List<Resolution>> AspectRatios { get; set; }
        }

        public AspectRatioData LoadAspectRatiosFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var aspectRatioData = JsonConvert.DeserializeObject<AspectRatioData>(json);
            return aspectRatioData;
        }
    }
}
