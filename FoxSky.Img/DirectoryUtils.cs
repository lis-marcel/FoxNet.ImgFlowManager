using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img
{
    public class DirectoryUtils
    {
        #region Public methods
        public static string CreateYearDstDir(string dstRootPath, DateTime? imgDate)
        {
            var dstRoot = imgDate.HasValue ?
                Path.Combine(dstRootPath, imgDate.Value.Year.ToString()) :
                dstRootPath;

            if (!System.IO.Directory.Exists(dstRoot))
            {
                System.IO.Directory.CreateDirectory(dstRoot);
            }

            return dstRoot;
        }
        public string CreateImageProcessingErrorDir(string dstRootPath)
        {
            string errorDirPath = Path.Combine(dstRootPath, "_error");

            if (!Directory.Exists(errorDirPath))
                Directory.CreateDirectory(errorDirPath);

            return errorDirPath;
        }
        #endregion
    }
}
