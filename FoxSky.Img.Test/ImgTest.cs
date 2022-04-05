using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FoxSky.Img.Test
{
    [TestClass]
    public class ImgTest
    {
        #region Consts
        const string srcFilePath = @".\Input\";
        const string dstFilePath = @".\Output\";
        #endregion

        #region Test methods
        [TestMethod]
        [DataRow(srcFilePath)]
        public void GetFilesFromSrcTest(string srcFile)
        {

        }
        #endregion

        #region Private methods
        #endregion
    }
}