using ExifLib;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace FoxSky.Img
{
    public enum Mode { Copy, Move }

    public class ImgMigrator
    {
        #region Properties
        public string? PicsOwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public Mode Mode { get; set; }

        private static string? _errorPath;
        private static readonly object _lock = new();
        private static string _apiKey => "AIzaSyD_cpKBl4fKo8ASfe0ubQYHhRWbX_IpoSU";
        private ConcurrentDictionary<string, object> _mutexes = new();
        #endregion

        #region Public methods
        public ImgMigrator(string picsOwnerSurname, string srcPath, string dstRootPath)
        {
            PicsOwnerSurname = picsOwnerSurname;
            SrcPath = srcPath;
            DstRootPath = dstRootPath;

            _errorPath = CreateImageProcessingErrorDir();
        }

        public bool StartProcessing()
        {
            if (File.Exists(SrcPath))
                return ProcessImageFile(SrcPath);
            else if (System.IO.Directory.Exists(SrcPath))
                return ProcessDirectory(SrcPath);
            else
            {
                LogError($"{SrcPath} is not a valid file or directory.");
                return false;
            }
        }
        public static void LogSuccess(string message)
        {
            lock (_lock)
            {
                Console.WriteLine($"[{DateTime.Now}]{"\u001b[32m"}Success! {"\u001b[0m"}{message}");
                Debug.WriteLine(message);
            }
        }
        public static void LogError(string message)
        {
            lock (_lock)
            {
                Console.WriteLine($"[{DateTime.Now}]{"\u001b[31m"}Error! {"\u001b[0m"}{message}");
                Debug.WriteLine(message);
            }
        }
        #endregion

        #region Private methods
        private bool ProcessImageFile(string srcImgPath)
        {
            try
            {
                var photoDate = ExtractPhotoDateFromExif(srcImgPath);
                var dstYearDir = CreateYearDstDir(photoDate);
                var dstFilePath = PrepareNewFileName(srcImgPath, dstYearDir, photoDate);

                switch (Mode)
                {
                    case Mode.Move:
                        MoveImg(srcImgPath, dstFilePath, true);
                        break;

                    case Mode.Copy:
                        CopyImg(srcImgPath, dstFilePath, true);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported mode {Mode}");
                }

                LogSuccess($"{srcImgPath} → {dstFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"During processing {srcImgPath} an error occurred: {ex.Message}");
                return false;
            }

            return true;
        }
        private bool ProcessDirectory(string targetDirectory)
        {
            //Process found files
            IEnumerable<string> files = System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpg", SearchOption.AllDirectories)
                .Union(System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpeg", SearchOption.AllDirectories));
            int filesCount = files.Count();
            int processedCount = 0;
            bool success = true;

            if (filesCount == 0)
            {
                LogSuccess("Nothing to do. No images found.");
                return !success;
            }

            LogSuccess($"Found {filesCount} images.");

            Parallel.ForEach(files, fileName => 
            {
                if (ProcessImageFile(fileName))
                {
                    processedCount++;
                }
            });

            success = processedCount == filesCount;

            if (success)
            {
                LogSuccess($"All {filesCount} files processedCount succesfully.");
            }
            else
            {
                LogError($"{filesCount - processedCount} of {filesCount} could not be processedCount.");
            }

            return success;
        }
        private static void MoveImg(string imgPath, string dstPath, bool overWrite)
        {
            File.Move(imgPath, dstPath, overWrite);
        }
        private static void CopyImg(string imgPath, string dstPath, bool overWrite)
        {
            File.Copy(imgPath, dstPath, overWrite);
        }
        private string CreateYearDstDir(DateTime? imgDate)
        {
            var dstRoot = imgDate.HasValue ?
                Path.Combine(DstRootPath, imgDate.Value.Year.ToString()) :
                DstRootPath;

            if (!System.IO.Directory.Exists(dstRoot))
            {
                System.IO.Directory.CreateDirectory(dstRoot);
            }

            return dstRoot;
        }
        private string CreateImageProcessingErrorDir()
        {
            string errorDirPath = Path.Combine(DstRootPath, "_error");

            if (!Directory.Exists(errorDirPath))
                Directory.CreateDirectory(errorDirPath);

            return errorDirPath;
        }
        private string PrepareNewFileName(string srcImgName, string dstImgPath, DateTime? imgDate)
        {
            var country = ReverseGeolocationRequestTask(srcImgName);

            var fileName = PicsOwnerSurname + (imgDate.HasValue ?
                imgDate.Value.ToString("yyyy-MM-dd HH-mm-ss") + country :
                Path.GetFileNameWithoutExtension(srcImgName));

            var processedFileName = RemoveTextSpaces(fileName);

            var extension = Path.GetExtension(srcImgName).Trim();
            var newFileName = Path.Combine(dstImgPath, processedFileName) + extension;
            int i = 1;

            while (File.Exists(newFileName))
            {
                bool differs = false;
                lock (_mutexes.GetOrAdd(newFileName, (s) => new object()))
                {
                    differs = CheckFileDiffers(srcImgName, newFileName);
                }

                if (!differs) break;

                newFileName = Path.Combine(dstImgPath, $"{fileName}_{i}") + extension;
                i++;
            }

            return newFileName;
        }
        private static string ReverseGeolocationRequestTask(string imgPath)
        {
            var location = CreateLocation(imgPath);

            if (location == null)
            {
                return string.Empty;
            }

            LocationGeocodeRequest locationGeocodeRequest = new()
            {
                Key = _apiKey,
                Location = location
            };

            var apiResponse = GoogleApi.GoogleMaps.Geocode.LocationGeocode.QueryAsync(locationGeocodeRequest).Result;

            if (apiResponse.Status != GoogleApi.Entities.Common.Enums.Status.Ok)
            {
                Console.WriteLine($"Could not get resutls, Status: {apiResponse.Status}");
                return string.Empty;
            }

            var deserializedResponse = JsonConvert.DeserializeObject<Root>(apiResponse.RawJson.ToString());

            var queryResult = deserializedResponse?.results?.FirstOrDefault()?.address_components
                ?.Where(ac => ac.types.Contains("country") || ac.types.Contains("locality"))
                ?.ToDictionary(ac => ac.types.Contains("country") ? "Country" : "City", ac => ac.long_name);

            string? country = queryResult.TryGetValue("Country", out var countryValue)
                ? ReplaceSpecialCharacters(countryValue)
                : null;
            string? city = queryResult.TryGetValue("City", out var cityValue)
                ? ReplaceSpecialCharacters(cityValue)
                : null;

            if (city != null && country != null)
            {
                return new StringBuilder()
                    .Append(city)
                    .Append(country)
                    .ToString();
            }

            return string.Empty;
        }
        static string ReplaceSpecialCharacters(string input)
        {
            string normalizedString = input.Normalize(NormalizationForm.FormKD);

            StringBuilder result = new();
            foreach (char c in normalizedString)
            {
                if (c == 'ł' || c == 'Ł')
                {
                    result.Append('l');
                }
                else
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (category != UnicodeCategory.NonSpacingMark)
                    {
                        result.Append(c);
                    }
                }
            }

            return result.ToString();
        }
        static string RemoveTextSpaces(string imgName)
        {
            var words = imgName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var processedFileName = string.Join("", words);

            return processedFileName;
        }
        private static Coordinate? CreateLocation(string imgPath)
        {
            using var reader = new ExifReader(imgPath);
            if (reader.GetTagValue(ExifTags.GPSLatitude, out double[] latitudeTab) &&
                reader.GetTagValue(ExifTags.GPSLongitude, out double[] longitudeTab))
            {
                double lat = latitudeTab[0] + (latitudeTab[1] / 60) + (latitudeTab[2] / 3600);
                double lon = longitudeTab[0] + (longitudeTab[1] / 60) + (longitudeTab[2] / 3600);

                Coordinate coordinate = new(lat, lon);

                return coordinate;
            }

            else return null;
        }
        private static bool CheckFileDiffers(string srcImgName, string dstImgName)
        {
            return !File.Exists(dstImgName) ||
                new FileInfo(srcImgName).Length != new FileInfo(dstImgName).Length ||
                !SameBinaryContent(srcImgName, dstImgName);
        }
        private static bool SameBinaryContent(string imgName1, string imgName2)
        {
            int file1byte;
            int file2byte;

            using (
                FileStream
                    fileStream1 = new(imgName1, FileMode.Open),
                    fileStream2 = new(imgName2, FileMode.Open))
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

                return true;
            }
        }
        private static DateTime? ExtractPhotoDateFromExif(string srcImgPath)
        {
            try
            {
                using var reader = new ExifReader(srcImgPath);
                if (reader.GetTagValue(ExifTags.DateTimeOriginal, out DateTime dateTimeOriginal))
                {
                    return dateTimeOriginal;
                }
                else
                {
                    LogError($"{srcImgPath} → File creaton time information not found in the image.");
                }
            }
            catch (Exception ex)
            {
                LogError($"{srcImgPath} → Error occured during reading Exif data: {ex.Message}");
            }

            return null;
        }

        #endregion
    }
}