using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Text;

namespace FoxSky.Img.FileProcessors
{
    public class FileHandler
    {
        private readonly GeolocationService geolocationService;
        private static readonly Dictionary<OperationMode, Action<string, string>> fileOperations = new()
        {
            { OperationMode.Copy, (src, dst) => File.Copy(src, dst, true) },
            { OperationMode.Move, (src, dst) => File.Move(src, dst, true) },
        };  

        public FileHandler(GeolocationService geolocationService)
        {
            this.geolocationService = geolocationService;
        }

        public async Task<int> ProcessDirectory(ImageProcessor processor)
        {
            // Check if directory exists before proceeding
            if (!System.IO.Directory.Exists(processor.SrcPath))
            {
                Logger.LogError($"Directory not found: {processor.SrcPath}");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }
            // Check if destination directory exists before proceeding
            if (!System.IO.Directory.Exists(processor.DstRootPath!))
            {
                Logger.LogError($"Destination directory not found: {processor.DstRootPath}");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }

            Logger.LogSuccess($"Processing: {processor.SrcPath}");

            try
            {
                var extensions = FileExtensionExtenstions.GetExtensions();
                var files = extensions.SelectMany(ext =>
                    System.IO.Directory.EnumerateFiles(processor.SrcPath, "*" + ext, SearchOption.AllDirectories));

                var filesCount = files.Count();
                int processed = 0;
                Logger.LogSuccess($"Found {filesCount} images.");

                foreach (var fileName in files)
                {
                    if (await ProcessImageFile(fileName, processor) == (int)EnviromentExitCodes.ExitCodes.Succcess)
                    {
                        processed++;
                    }
                }

                if (filesCount == 0)
                {
                    Logger.LogSuccess("Nothing to do. No images found.");
                }
                else if (processed == filesCount)
                {
                    Logger.LogSuccess($"All {filesCount} files processed successfully.");
                }
                else
                {
                    Logger.LogError($"{filesCount - processed} of {filesCount} could not be processed.");
                    return (int)EnviromentExitCodes.ExitCodes.Warninig;
                }

                return (int)EnviromentExitCodes.ExitCodes.Succcess;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing directory {processor.SrcPath}: {ex.Message}");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }
        }

        public async Task<int> ProcessImageFile(string srcFilePath, ImageProcessor processor)
        {
            try
            {
                var photoDateTime = ExtractPhotoDateFromExif(srcFilePath);
                var dstPath = PrepareDstDir(photoDateTime, processor.DstRootPath!);
                var dstFilePath = await PrepareNewFileName(srcFilePath, dstPath, photoDateTime, processor);

                if (fileOperations.TryGetValue(processor.Mode, out var fileOperation))
                {
                    fileOperation(srcFilePath, dstFilePath);
                }
                else
                {
                    Logger.LogError($"Unsupported mode: {processor.Mode}");
                    return (int)EnviromentExitCodes.ExitCodes.Error;
                }

                Logger.LogSuccess($"{srcFilePath} -> {dstFilePath}");
                return (int)EnviromentExitCodes.ExitCodes.Succcess;
            }
            catch (Exception ex)
            {
                Logger.LogError($"During processing {srcFilePath} an error occurred: {ex.Message}");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }
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
            string location = string.Empty;
            StringBuilder sb = new();

            if (processor.GeolocationFlag)
            {
                location = TextUtils.RemoveSpaces(await geolocationService.ReverseGeolocationRequestTask(srcFileName, processor.UserEmail!, processor.Radius!));
            }

            sb.Append(processor.PicsOwnerSurname);
            sb.Append('_');
            sb.Append(photoDate.HasValue ?
                photoDate.Value.ToString("yyyy-MM-dd HH-mm-ss") + location :
                Path.GetFileNameWithoutExtension(srcFileName));

            string fileName = sb.ToString();

            var extension = Path.GetExtension(srcFileName).Trim();

            // If the extension is .jpeg, change it to .jpg
            if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".jpg";
            }

            var newFileName = Path.Combine(dstPath, fileName) + extension;
            int fileDuplicatesCounter = 1;

            while (File.Exists(newFileName))
            {
                var differs = CheckFileDiffers(srcFileName, newFileName);

                if (!differs) break;

                newFileName = Path.Combine(dstPath, $"{fileName}_{fileDuplicatesCounter}") + extension;
                fileDuplicatesCounter++;
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
