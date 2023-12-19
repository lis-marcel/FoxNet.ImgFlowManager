﻿using ExifLib;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace FoxSky.Img
{
    public enum Mode { Copy, Move }

    public class ImgMigrator
    {
        #region Properties
        public string? PicsOwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public string ApiKey
        {
            get => "AIzaSyD_cpKBl4fKo8ASfe0ubQYHhRWbX_IpoSU";
        }
        public Mode Mode { get; set; }
        private static readonly object Lock = new();
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

        public bool ProcessImageFile(string imgName)
        {
            try
            {
                var photoDate = ExtractPhotoDateFromExif(imgName);
                var dstPath = PrepareDstDir(photoDate);
                var dstFileName = PrepareNewFileName(imgName, dstPath, photoDate);

                switch (Mode)
                {
                    case Mode.Move:
                        File.Move(imgName, dstFileName, true);
                        break;

                    case Mode.Copy:
                        File.Copy(imgName, dstFileName, true);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported mode {Mode}");
                }

                LogSuccess($"{imgName} → {dstFileName}");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"During processing {imgName} an error occured: {ex.Message}");

                return false;
            }
        }

        public bool ProcessDirectory(string targetDirectory)
        {
            //Process found files
            var files = System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpg", SearchOption.AllDirectories)
                .Union(System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpeg", SearchOption.AllDirectories));

            var filesCount = files.Count();
            int processed = 0;
            LogSuccess($"Found {filesCount} images.");

            Parallel.ForEach(files, fileName =>
            {
                if (ProcessImageFile(fileName))
                {
                    processed++;
                }
            });

            var success = processed == filesCount;

            if (filesCount == 0)
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

        public static void LogSuccess(string message)
        {
            lock (Lock)
            {
                try
                {
                    Console.Write($"[{DateTime.Now}]");
                    Console.Write($"{"\u001b[32m"}");
                    Console.Write("Success! ");
                    Console.Write($"{"\u001b[0m"}");
                    Console.Write($"{message}");
                    Debug.WriteLine(message);
                    Console.WriteLine();
                }
                finally
                {
                    Console.ResetColor();
                }
            }

        }

        public static void LogError(string message)
        {
            lock (Lock)
            {
                try
                {
                    Console.Write($"[{DateTime.Now}]");
                    Console.Write($"{"\u001b[31m"}");
                    Console.Write("Error! ");
                    Console.Write($"{"\u001b[0m"}");
                    Console.Write($"{message}");
                    Debug.WriteLine(message);
                    Console.WriteLine();
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
        #endregion

        #region Private methods
        private string PrepareDstDir(DateTime? imgDate)
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

        private string PrepareNewFileName(string srcImgName, string dstImgPath, DateTime? imgDate)
        {
            var country = ReverseGeolocationRequestTask(srcImgName).Result;

            var fileName = PicsOwnerSurname + (imgDate.HasValue ?
                imgDate.Value.ToString("yyyy-MM-dd HH-mm-ss") + country :
                Path.GetFileNameWithoutExtension(srcImgName));

            var processedFileName = RemoveTextSpaces(fileName);

            var extension = Path.GetExtension(srcImgName).Trim();
            var newFileName = Path.Combine(dstImgPath, processedFileName) + extension;
            int i = 1;

            while (File.Exists(newFileName))
            {
                var differs = CheckFileDiffers(srcImgName, newFileName);

                if (!differs) break;

                newFileName = Path.Combine(dstImgPath, $"{fileName}_{i}") + extension;
                i++;
            }

            return newFileName;
        }

        private async Task<string> ReverseGeolocationRequestTask(string imgPath)
        {
            var location = CreateLocation(imgPath);

            if (location == null)
            {
                return string.Empty;
            }

            LocationGeocodeRequest locationGeocodeRequest = new()
            {
                Key = ApiKey,
                Location = location
            };

            var apiResponse = await GoogleApi.GoogleMaps.Geocode.LocationGeocode.QueryAsync(locationGeocodeRequest);

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

            var result = new StringBuilder();
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

            using FileStream fileStream1 = new(imgName1, FileMode.Open),
                fileStream2 = new(imgName2, FileMode.Open);
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

        private static DateTime? ExtractPhotoDateFromExif(string imgName)
        {
            try
            {
                using var reader = new ExifReader(imgName);
                if (reader.GetTagValue(ExifTags.DateTimeOriginal, out DateTime dateTimeOriginal))
                {
                    return dateTimeOriginal;
                }
                else
                {
                    LogError($"{imgName} → File creaton time information not found in the image.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"{imgName} → Error occured during reading Exif data: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}