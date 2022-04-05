using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace FoxSky.Img
{
    public class ImgMigrator
    {
        #region Properties
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public string? Prefix { get; set; }
        #endregion

        
        #region Public methods
        public void GetFilesFromSrc(string srcPath)
        {
            var strPath = srcPath.ToString();
            
            if (File.Exists(strPath)) 
                ProcessFile(strPath);
            else if (System.IO.Directory.Exists(strPath)) 
                ProcessDirectory(strPath);
            else 
                Console.WriteLine("{0} is not a valid file or directory.", srcPath);
        }

        public void ProcessFile(string fileName)
        {
            var photoDate = ExtractPhotoDateFromExif(fileName);
            var dstPath = PrepareDstDir(photoDate);
            var dstFileName = PrepareNewFileName(fileName, dstPath, photoDate);

            File.Move(fileName, dstFileName, true);

            Console.WriteLine("Processed file '{0}'.", fileName);
        }

        public void ProcessDirectory(string targetDirectory)
        {
            //Process found files
            var files = System.IO.Directory.EnumerateFiles(targetDirectory, "*.jpg", SearchOption.AllDirectories);
            
            foreach (var fileName in files)
            {
                ProcessFile(fileName);
            }
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
            int file1byte = 0;
            int file2byte = 0;

            using (FileStream fileStream1 = new FileStream(fileName1, FileMode.Open),
                fileStream2 = new FileStream(fileName2, FileMode.Open))
            {
                do
                {
                    file1byte = fileStream1.ReadByte();
                    file2byte =  fileStream2.ReadByte();
                }
                while (file1byte == file2byte&& file1byte != -1);

                return true;
            }

            return false; 
        }
        
        private DateTime? ExtractPhotoDateFromExif(string fileName)
        {
            DateTime dateTime;

            return ImageMetadataReader.ReadMetadata(fileName)?
                .OfType<ExifSubIfdDirectory>()?
                .FirstOrDefault()?
                .TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out dateTime) == true ? dateTime : null;

        }

        #endregion
    }
}
