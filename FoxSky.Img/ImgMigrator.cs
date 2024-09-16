using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Diagnostics;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using System.Globalization;
using System.Text;
using GoogleApi.Entities.Common;
using System.Device.Location;
using static System.Net.Mime.MediaTypeNames;

namespace FoxSky.Img
{
    public enum Mode { Move, Copy }

    public class ImgMigrator
    {
        #region Private fields
        private string? picsOwnerSurname;
        private string? srcPath;
        private string? dstRootPath;
        private double distance;
        private static string apiKey
        {
            get => "AIzaSyD_cpKBl4fKo8ASfe0ubQYHhRWbX_IpoSU";
        }

        private static List<Tuple<Coordinate, string>> locationCache = [];
        #endregion

        #region Public properties
        public string? PicsOwnerSurname 
        { 
            get => picsOwnerSurname;
            set
            {
                picsOwnerSurname = value; 
            }
        }
        public string? SrcPath
        {
            get => srcPath;
            set
            {
                srcPath = value;
            }
        }
        public string? DstRootPath
        {
            get => dstRootPath;
            set
            {
                dstRootPath = value;
            }
        }
        public string? Distance
        {
            get => distance.ToString();
            set
            {
                if (value != null)
                    distance = double.Parse(value);
            }
        }
        public Mode Mode { get; set; }
        #endregion
        
        #region Public methods
        public async Task<bool> ProcessImages()
        {
            bool processResult = false;

            if (File.Exists(srcPath)) 
                processResult = await ProcessImageFile(srcPath);
            else if (System.IO.Directory.Exists(srcPath)) 
                processResult = await ProcessDirectory(srcPath);
            else
                LogError($"{srcPath} is not a valid file or directory.");

            return processResult;
        }

        public async Task<bool> ProcessImageFile(string srcFilePath)
        {
            try
            {
                var photoDateTime = ExtractPhotoDateFromExif(srcFilePath);
                var dstPath = PrepareDstDir(photoDateTime);
                var dstFilePath = await PrepareNewFileName(srcFilePath, dstPath, photoDateTime);

                switch (Mode)
                {
                    case Mode.Move:
                        File.Move(srcFilePath, dstFilePath, true);
                        break;

                    case Mode.Copy:
                        File.Copy(srcFilePath, dstFilePath, true);
                        break;
                    
                    default:
                        throw new ArgumentException($"Unsupported mode {Mode}");
                }

                LogSuccess($"{srcFilePath} → {dstFilePath}");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"During processing {srcFilePath} an error occured: {ex.Message}");

                return false;
            }
        }

        public async Task<bool> ProcessDirectory(string targetDirectory)
        {
            LogSuccess($"Processing: {targetDirectory}");

            //Process found files
            var files = System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpg", SearchOption.AllDirectories)
                .Union(System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpeg", SearchOption.AllDirectories));

            var filesCount = files.Count();
            int processed = 0;
            LogSuccess($"Found {filesCount} images.");

            foreach (var fileName in files)
            {
                if (await ProcessImageFile(fileName))
                {
                    processed++;
                }
            }

            var success = processed == filesCount;

            if (filesCount == 0 )
            {
                LogSuccess("Nothing to do. No images found.");
            }
            else if (success)
            {
                LogSuccess($"All {filesCount} files processed succesfully.");
            }
            else 
            {
                LogError($"{filesCount - processed} of {filesCount} could not be processed.");
            }

            return success;
        }

        #endregion

        #region Private methods
        private string PrepareDstDir(DateTime? photoDate)
        {
            var dstRoot = photoDate.HasValue ?
                Path.Combine(dstRootPath, photoDate.Value.Year.ToString()) :
                dstRootPath;

            if (!System.IO.Directory.Exists(dstRoot))
            {
                System.IO.Directory.CreateDirectory(dstRoot);
            }

            return dstRoot;
        }

        private async Task<string> PrepareNewFileName(string srcFileName, string dstPath, DateTime? photoDate)
        {
            var place = RemoveSpaces(await ReverseGeolocationRequestTask(srcFileName));

            var fileName = picsOwnerSurname + "_" + (photoDate.HasValue ?
                photoDate.Value.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + place :
                Path.GetFileNameWithoutExtension(srcFileName));

            var extension = Path.GetExtension(srcFileName).Trim();
            var newFileName = Path.Combine(dstPath, fileName) + extension;
            int i = 1;

            while (File.Exists(newFileName))
            {
                var differs = CheckFileDiffers(srcFileName, newFileName);

                if (!differs) break;

                newFileName = Path.Combine(dstPath, $"{fileName}_{i}") + extension;
                i++;
            }
            
            return newFileName;
        }

        private async Task<string> ReverseGeolocationRequestTask(string imagePath)
        {
            var location = CreateLocation(imagePath);

            if (location != null)
            {
                var cachedLocation = GetCachedLocation(location);

                if (!string.IsNullOrEmpty(cachedLocation))
                {
                    return cachedLocation;
                }

                LocationGeocodeRequest locationGeocodeRequest = new()
                {
                    Key = apiKey,
                    Location = location
                };
                StringBuilder sb = new();
                string fullLocationName = string.Empty, 
                    city = string.Empty, 
                    country = string.Empty;

                var apiGeolocationResponse = await GoogleApi.GoogleMaps.Geocode.LocationGeocode.QueryAsync(locationGeocodeRequest);

                if (apiGeolocationResponse.Status == GoogleApi.Entities.Common.Enums.Status.Ok)
                {
                    foreach (var result in apiGeolocationResponse.Results)
                    {
                        foreach (var addressComponent in result.AddressComponents)
                        {
                            foreach (var type in addressComponent.Types)
                            {
                                if (type.ToString() == "Locality")
                                {
                                    city = ReplaceSpecialCharacters(addressComponent.LongName);
                                }
                                else if (type.ToString() == "Country")
                                {
                                    country = ReplaceSpecialCharacters(addressComponent.LongName);
                                }

                                if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
                                {
                                    sb.Append(city);
                                    sb.Append('_');
                                    sb.Append(country);

                                    fullLocationName = sb.ToString();

                                    AddLocationToCache(location, fullLocationName);

                                    return fullLocationName;
                                }

                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Could not get resutls, Status: {apiGeolocationResponse.Status}");
                }
            }

            return string.Empty;
        }

        private static string ReplaceSpecialCharacters(string input)
        {
            Dictionary<char, char> characterReplacements = new()
            {
                {'Ł', 'L'},
                {'ł', 'l'}
            };

            StringBuilder result = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                if (characterReplacements.TryGetValue(c, out char replacement))
                {
                    result.Append(replacement);
                }
                else
                {
                    string normalized = c.ToString().Normalize(NormalizationForm.FormD);

                    foreach (char ch in normalized)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                        {
                            result.Append(ch);
                        }
                    }
                }
            }

            return result.ToString();
        }

        private static string RemoveSpaces(string s)
        {
            return !string.IsNullOrEmpty(s) ? s.Replace(" ", "") : s;
        }

        private static void AddLocationToCache(Coordinate location, string locationName)
        {
            if (!string.IsNullOrEmpty(locationName))
            {
                locationCache.Add(new (location, locationName));
            }
        }

        private string? GetCachedLocation(Coordinate location)
        {
            return locationCache.FirstOrDefault(c => IsWithinDistance(location, c.Item1, distance))?.Item2;
        }

        private static bool IsWithinDistance(Coordinate loc1, Coordinate loc2, double distanceInMeters)
        {
            var coord1 = new GeoCoordinate(loc1.Latitude, loc1.Longitude);
            var coord2 = new GeoCoordinate(loc2.Latitude, loc2.Longitude);
            return coord1.GetDistanceTo(coord2) <= distanceInMeters;
        }

        private static Coordinate? CreateLocation(string imgPath)
        {
            Coordinate? res = null;
            
            var gps = ImageMetadataReader.ReadMetadata(imgPath)?
                .OfType<GpsDirectory>()?
                .FirstOrDefault();

            var location = gps?.GetGeoLocation();

            if (location != null)
            {
                double lat = location.Latitude;
                double lon = location.Longitude;

                res = new(lat, lon);
            }

            return res;
        }

        private bool CheckFileDiffers(string srcFileName, string dstFileName)
        {
            return !File.Exists(dstFileName) ||
                new FileInfo(srcFileName).Length != new FileInfo(dstFileName).Length ||
                !SameBinaryContent(srcFileName, dstFileName);
        }

        private bool SameBinaryContent(string fileName1, string fileName2)
        {
            int file1byte;
            int file2byte;

            using (FileStream fileStream1 = new FileStream(fileName1, FileMode.Open),
                fileStream2 = new FileStream(fileName2, FileMode.Open))
            {
                while (true)
                {
                    file1byte = fileStream1.ReadByte();
                    file2byte = fileStream2.ReadByte();

                    if (file1byte != file2byte)
                    {
                        return false;
                    }

                    if (file1byte == file2byte && file1byte == -1)
                    {
                        break;
                    }
                }
            }

            return true;
        }

        private DateTime? ExtractPhotoDateFromExif(string fileName)
        {
            DateTime dateTime;

            return ImageMetadataReader.ReadMetadata(fileName)?
                .OfType<ExifSubIfdDirectory>()?
                .FirstOrDefault()?
                .TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out dateTime) == true ? dateTime : null;
        }

        public static void LogSuccess(string message)
        {
            try
            {
                Console.Write($"[{DateTime.Now}]");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Success! ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void LogError(string message)
        {
            try
            {
                Console.Write($"[{DateTime.Now}]");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Error! ");
                Console.Write($"{message}");
                Debug.WriteLine(message);
                Console.WriteLine();
            }
            finally
            {
                Console.ResetColor();
            }
        }
        #endregion
    }
}