namespace FoxSky.Img
{

    public class ImgMigrator : FileUtils
    {
        #region Properties
        public string? PicsOwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        #endregion

        #region Constructors
        public ImgMigrator(string picsOwnerSurname, string srcPath, string dstRootPath)
        {
            PicsOwnerSurname = picsOwnerSurname;
            SrcPath = srcPath;
            DstRootPath = dstRootPath;
        }
        #endregion

        #region Public methods
        public bool StartProcessing()
        {
            if (File.Exists(SrcPath))
                return ProcessImageFile(PicsOwnerSurname, SrcPath, DstRootPath);
            else if (System.IO.Directory.Exists(SrcPath))
                return ProcessDirectory(SrcPath);
            else
            {
                Logger.LogError($"{SrcPath} is not a valid file or directory.");
                return false;
            }
        }
        #endregion

        #region Private methods
        private bool ProcessDirectory(string dstDirectory)
        {
            //Process found files
            IEnumerable<string> files = System.IO.Directory.EnumerateFiles(dstDirectory, "*.jpg", SearchOption.AllDirectories)
                .Union(System.IO.Directory.EnumerateFiles(dstDirectory, "*.jpeg", SearchOption.AllDirectories));
            int filesCount = files.Count();
            int processedCount = 0;
            bool success = true;

            if (filesCount == 0)
            {
                Logger.LogSuccess("Nothing to do. No images found.");
                return !success;
            }

            Logger.LogSuccess($"Found {filesCount} images.");

            Parallel.ForEach(files, fileName => 
            //files.ToList().ForEach(fileName =>
            {
                if (ProcessImageFile(PicsOwnerSurname, fileName, DstRootPath))
                {
                    processedCount++;
                }
            });

            success = processedCount == filesCount;

            if (success)
            {
                Logger.LogSuccess($"All {filesCount} files processedCount succesfully.");
            }
            else
            {
                Logger.LogError($"{filesCount - processedCount} of {filesCount} could not be processed.");
            }

            return success;
        }
        #endregion
    }
}