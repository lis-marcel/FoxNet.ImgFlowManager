using ExifLib;
using System.Collections.Concurrent;

namespace FoxSky.Img
{
    public enum Mode { Copy, Move }

    public class FileUtils
    {
        #region Properties
        public Mode Mode { get; set; }

        private ConcurrentDictionary<string, object> _mutexes = new();
        #endregion

        #region Public methods
        public bool ProcessImageFile(string picsOwnerSurname, string srcImgPath, string dstRootPath)
        {
            try
            {
                var photoDate = ExtractPhotoDateFromExif(srcImgPath);
                var dstYearDir = DirectoryUtils.CreateYearDstDir(dstRootPath, photoDate);
                var dstFilePath = PrepareNewFileName(picsOwnerSurname, srcImgPath, dstYearDir, photoDate);

                switch (Mode)
                {
                    case Mode.Move:
                        MoveFile(srcImgPath, dstFilePath, true);
                        break;

                    case Mode.Copy:
                        CopyFile(srcImgPath, dstFilePath, true);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported mode {Mode}");
                }

                Logger.LogSuccess($"{srcImgPath} → {dstFilePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"During processing {srcImgPath} an error occurred: {ex.Message}");
                return false;
            }

            return true;
        }
        #endregion

        #region Private methods
        private static void MoveFile(string srcPath, string dstPath, bool overwrite)
        {
            File.Move(srcPath, dstPath, overwrite);
        }
        private static void CopyFile(string srcPath, string dstPath, bool overwrite)
        {
            File.Copy(srcPath, dstPath, overwrite);
        }
        private static bool CheckFileDiffers(string srcImgName, string dstImgName)
        {
            return !File.Exists(dstImgName) ||
                new FileInfo(srcImgName).Length != new FileInfo(dstImgName).Length ||
                !SameBinaryContent(srcImgName, dstImgName);
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
                    Logger.LogError($"{srcImgPath} → File creaton time information not found in the image.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{srcImgPath} → Error occured during reading Exif data: {ex.Message}");
            }

            return null;
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
        private string PrepareNewFileName(string picsOwnerSurname, string srcImgName, string dstImgPath, DateTime? imgDate)
        {
            var country = GeolocationUtils.ReverseGeolocationRequestTask(srcImgName)?.RemoveTextSpaces();
            var dateSignature = imgDate.HasValue ? imgDate.Value.ToString("yyyy-MM-dd HH-mm-ss") : Path.GetFileNameWithoutExtension(srcImgName);
            var fileName = $"{picsOwnerSurname}_{dateSignature}_{country}";

            var extension = Path.GetExtension(srcImgName).Trim();
            var newFileName = Path.Combine(dstImgPath, fileName) + extension;
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
        #endregion
    }
}