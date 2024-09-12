using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Diagnostics;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using System.Globalization;
using System.Text;
using GoogleApi.Entities.Common;
using static System.Net.Mime.MediaTypeNames;

namespace FoxSky.Img
{
    public enum Mode { Move, Copy }

    public class ImgMigrator
    {
        #region Properties
        public string? PicsOwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public static string ApiKey 
        { 
            get => "AIzaSyD_cpKBl4fKo8ASfe0ubQYHhRWbX_IpoSU"; 
        }
        public Mode Mode { get; set; }
        #endregion
        
        #region Public methods
        public bool ProcessImages()
        {
            bool res = false;

            if (File.Exists(SrcPath)) 
                res = ProcessImageFile(SrcPath);
            else if (System.IO.Directory.Exists(SrcPath)) 
                res = ProcessDirectory(SrcPath);
            else
                LogError($"{SrcPath} is not a valid file or directory.");

            return res;
        }
        public bool ProcessImageFile(string fileName)
        {
            try
            {
                var photoDate = ExtractPhotoDateFromExif(fileName);
                var dstPath = PrepareDstDir(photoDate);
                var dstFileName = PrepareNewFileName(fileName, dstPath, photoDate);

                switch (Mode)
                {
                    case Mode.Move:
                        File.Move(fileName, dstFileName, true);
                        break;

                    case Mode.Copy:
                        File.Copy(fileName, dstFileName, true);
                        break;
                    
                    default:
                        throw new ArgumentException($"Unsupported mode {Mode}");
                }

                LogSuccess($"{fileName} → {dstFileName}");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"During processing {fileName} an error occured: {ex.Message}");

                return false;
            }
        }
        public bool ProcessDirectory(string targetDirectory)
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
                if (ProcessImageFile(fileName))
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
                Path.Combine(DstRootPath, photoDate.Value.Year.ToString()) :
                DstRootPath;

            if (!System.IO.Directory.Exists(dstRoot))
            {
                System.IO.Directory.CreateDirectory(dstRoot);
            }

            return dstRoot;
        }
        private string PrepareNewFileName(string srcFileName, string dstPath, DateTime? photoDate)
        {
            var place = ReverseGeolocationRequestTask(srcFileName).Result;

            var fileName = PicsOwnerSurname + "_" + (photoDate.HasValue ?
                photoDate.Value.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + place :
                Path.GetFileNameWithoutExtension(srcFileName));

            //var processedFileName = RemoveTextSpaces(fileName);

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
                LocationGeocodeRequest locationGeocodeRequest = new()
                {
                    Key = ApiKey,
                    Location = CreateLocation(imagePath)
                };
                string city = "",
                    country = "";

                var res = await GoogleApi.GoogleMaps.Geocode.LocationGeocode.QueryAsync(locationGeocodeRequest);

                if (res.Status == GoogleApi.Entities.Common.Enums.Status.Ok)
                {
                    foreach (var result in res.Results)
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

                                if (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(country))
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Could not get resutls, Status: {res.Status}");
                }

                return $"{city}_{country}";
            }

            else return "";
        }
        static string ReplaceSpecialCharacters(string input)
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
        static string RemoveTextSpaces(string fileName)
        {
            var words = fileName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var processedFileName = string.Join("", words);

            return processedFileName;
        }
        private static Coordinate? CreateLocation(string imgPath)
        {
            var gps = ImageMetadataReader.ReadMetadata(imgPath)?
                .OfType<GpsDirectory>()?
                .FirstOrDefault();

            if (gps != null)
            {
                var location = gps?.GetGeoLocation();

                double lat = location.Latitude;
                double lon = location.Longitude;

                Coordinate coordinate = new(lat, lon);

                return coordinate;
            }

            else return null;
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