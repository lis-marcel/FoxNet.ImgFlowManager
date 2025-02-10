using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using FoxSky.Img.FileProcessors;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;

namespace FoxSky.Img.Processors
{
    public class FileHandler
    {
        private readonly GeolocationService geolocationService;

        public FileHandler(GeolocationService geolocationService)
        {
            this.geolocationService = geolocationService;
        }

        public async Task<bool> ProcessImageFile(string srcFilePath, ImageProcessor processor)
        {
            try
            {
                var photoDateTime = ExtractPhotoDateFromExif(srcFilePath);
                var dstPath = PrepareDstDir(photoDateTime, processor.DstRootPath);
                var dstFilePath = await PrepareNewFileName(srcFilePath, dstPath, photoDateTime, processor);

                switch (processor.Mode)
                {
                    case Mode.Move:
                        File.Move(srcFilePath, dstFilePath, true);
                        break;

                    case Mode.Copy:
                        File.Copy(srcFilePath, dstFilePath, true);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported mode {processor.Mode}");
                }

                Logger.LogSuccess($"{srcFilePath} → {dstFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"During processing {srcFilePath} an error occurred: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProcessDirectory(string targetDirectory, ImageProcessor processor)
        {
            Logger.LogSuccess($"Processing: {targetDirectory}");

            var extensions = FileExtensionExtenstions.GetExtensions();
            var files = extensions.SelectMany(ext => 
                System.IO.Directory.EnumerateFiles(targetDirectory, "*" + ext, SearchOption.AllDirectories));

            var filesCount = files.Count();
            int processed = 0;
            Logger.LogSuccess($"Found {filesCount} images.");

            foreach (var fileName in files)
            {
                if (await ProcessImageFile(fileName, processor))
                {
                    processed++;
                }
            }

            var success = processed == filesCount;

            if (filesCount == 0)
            {
                Logger.LogSuccess("Nothing to do. No images found.");
            }
            else if (success)
            {
                Logger.LogSuccess($"All {filesCount} files processed successfully.");
            }
            else
            {
                Logger.LogError($"{filesCount - processed} of {filesCount} could not be processed.");
            }

            return success;
        }

        private static string PrepareDstDir(DateTime? photoDate, string dstRootPath)
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

        private async Task<string> PrepareNewFileName(string srcFileName, string dstPath, DateTime? photoDate, ImageProcessor processor)
        {
            var place = TextUtils.RemoveSpaces(await geolocationService.ReverseGeolocationRequestTask(srcFileName, processor.UserEmail, processor.Radius));

            var fileName = processor.PicsOwnerSurname + "_" + (photoDate.HasValue ?
                photoDate.Value.ToString("yyyy-MM-dd HH-mm-ss") + "_" + place :
                Path.GetFileNameWithoutExtension(srcFileName));

            var extension = Path.GetExtension(srcFileName).Trim();

            // If the extension is .jpeg, change it to .jpg
            if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".jpg";
            }

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

        private static bool CheckFileDiffers(string srcFileName, string dstFileName)
        {
            return !File.Exists(dstFileName) ||
                new FileInfo(srcFileName).Length != new FileInfo(dstFileName).Length ||
                !SameBinaryContent(srcFileName, dstFileName);
        }

        private static bool SameBinaryContent(string fileName1, string fileName2)
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

        private static DateTime? ExtractPhotoDateFromExif(string fileName)
        {
            DateTime dateTime;

            return ImageMetadataReader.ReadMetadata(fileName)?
                .OfType<ExifSubIfdDirectory>()?
                .FirstOrDefault()?
                .TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out dateTime) == true ? dateTime : null;
        }
    }
}
