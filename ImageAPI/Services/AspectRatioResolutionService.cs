using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


namespace ImageAPI.Services
{    public class AspectRatioResolutionService
    {
        // Define the structure for the aspect ratio data
        public class Resolution
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class AspectRatioData
        {
            public Dictionary<string, List<Resolution>> AspectRatios { get; set; }
        }

        // Method to load the aspect ratios and resolutions from the JSON file
        public AspectRatioData LoadAspectRatiosFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var aspectRatioData = JsonConvert.DeserializeObject<AspectRatioData>(json);
            return aspectRatioData;
        }
    }
}
