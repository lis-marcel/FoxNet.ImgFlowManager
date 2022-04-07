using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Diagnostics;

namespace FoxSky.Img
{
    public enum Mode { Move, Copy }

    public class ImgMigrator
    {
        #region Properties
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
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
                        throw new ArgumentException($"Unspported mode {Mode}");
                }

                Log($"{fileName} → {dstFileName}");

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
            //Process found files
            var files = System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpg", SearchOption.AllDirectories)
                .Union(System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpeg", SearchOption.AllDirectories));

            var filesCount = files.Count();
            int processed = 0;
            Log($"Found {filesCount} images.");

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
                Log("Nothing to do. No images found.");
            }
            else if (success)
            {
                Log($"All {filesCount} files processed succesfully.");
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
            var fileName = photoDate.HasValue ?
                photoDate.Value.ToString("yyyyMMdd_HHmmss") :
                Path.GetFileNameWithoutExtension(srcFileName);

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

        private bool CheckFileDiffers(string srcFileName, string dstFileName)
        {
            return !File.Exists(dstFileName) ||
                new FileInfo(srcFileName).Length != new FileInfo(dstFileName).Length ||
                SameBinaryContent(srcFileName, dstFileName);
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

        public static void Log(string message)
        {
            Console.Write($"[{DateTime.Now}]");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Success!");
            Console.Write($"{message}");
        }
        public static void LogError(string message)
        {
            Console.Write($"[{DateTime.Now}]");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Success!");
            Console.Write($"{message}");
            Debug.WriteLine(message);
        }

        #endregion
    }
}