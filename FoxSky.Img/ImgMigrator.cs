using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            else if (Directory.Exists(strPath)) 
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
            var files = Directory.EnumerateFiles(targetDirectory, "*.jpg");
            
            foreach (var fileName in files)
            {
                ProcessFile(fileName);
            }
        }
        #endregion

        #region Private methods
        private string PrepareDstDir(DateTime photoDate)
        {
            var dstRoot = Path.Combine(DstRootPath, photoDate.Year.ToString());

            if (!Directory.Exists(dstRoot))
            {
                Directory.CreateDirectory(dstRoot);
            }

            return dstRoot;
        }

        private string PrepareNewFileName(string srcFileName, string dstPath, DateTime photoDate)
        {
            var fileName = photoDate.ToString("yyyyMMdd_hhmmss");
            var extension = Path.GetExtension(fileName).Trim();
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
            return false; // TODO: compare binaries
        }
        
        private DateTime ExtractPhotoDateFromExif(string fileName)
        {
            var exif = File.GetCreationTimeUtc(fileName);

            return exif;
        }

        #endregion
    }
}
