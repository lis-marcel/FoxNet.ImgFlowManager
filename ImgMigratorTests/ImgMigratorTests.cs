using FoxSky.Img;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace ImgMigratorTests
{
    [TestClass]
    public class ImgMigratorTests
    {
        #region Consts
        private const string CORRECT_INPUT_FILE_PATH = @".\Input";
        private const string INCORRECT_INPUT_FILE_PATH = @".\no existing path";
        private const string DST_FILE_PATH = @".\existing";
        private const string FILE_1 = @".\Input\compare\file1.jpg";
        private const string FILE_1_PLUS = @".\Input\compare\file1_plus_one_byte.jpg";
        private const string TXT_FILE = @".\Input\images.txt";
        private const int VALID_IMAGES_COUNT = 6;
        #endregion

        #region Test methods

        //[TestCleanup]
        [TestInitialize]
        public void InitTest()
        {
            if (Directory.Exists(DST_FILE_PATH))
                Directory.Delete(DST_FILE_PATH, true);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public void UnpositiveProcessProcessImageFileTest(string correctSrcFile, string incorrectDstFilePath)
        {
            var imgMigartor = new ImgMigrator() { SrcPath = correctSrcFile, DstRootPath = incorrectDstFilePath, Mode = Mode.Copy };
            imgMigartor.ProcessImageFile(correctSrcFile);
        }

        [TestMethod]
        [DataRow(FILE_1, DST_FILE_PATH)]
        public void PositiveProcessImageFileTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { DstRootPath = dstFilePath, Mode = Mode.Copy };
            Assert.IsTrue(imgMigrator.ProcessImageFile(correctSrcFilePath));
            Assert.IsTrue(imgMigrator.ProcessImageFile(correctSrcFilePath));
            Assert.IsTrue(imgMigrator.ProcessImageFile(correctSrcFilePath));

            Assert.IsTrue(File.Exists(Path.Combine(dstFilePath, @"2019\20190218_161133.jpg")));

            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\20190218_161133_0.jpg")));
            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\20190218_161133_1.jpg")));
            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\20190218_161133_2.jpg")));
        }

        [TestMethod]
        public void PositiveProcessImageFilePlusTest()
        {
            var imgMigrator = new ImgMigrator() { DstRootPath = DST_FILE_PATH, Mode = Mode.Copy };
            Assert.IsTrue(imgMigrator.ProcessImageFile(FILE_1));
            Assert.IsTrue(imgMigrator.ProcessImageFile(FILE_1_PLUS));

            Assert.IsTrue(File.Exists(Path.Combine(imgMigrator.DstRootPath, @"2019\20190218_161133.jpg")));
            Assert.IsTrue(File.Exists(Path.Combine(imgMigrator.DstRootPath, @"2019\20190218_161133_1.jpg")));

            Assert.IsFalse(File.Exists(Path.Combine(imgMigrator.DstRootPath, @"2019\20190218_161133_0.jpg")));
            Assert.IsFalse(File.Exists(Path.Combine(imgMigrator.DstRootPath, @"2019\20190218_161133_2.jpg")));
        }

        [TestMethod]
        [DataRow(TXT_FILE, DST_FILE_PATH)]
        public void WrongExtensionTest(string incorrectSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = incorrectSrcFilePath, DstRootPath = dstFilePath, Mode = Mode.Copy };
            Assert.IsFalse(imgMigrator.ProcessImageFile(incorrectSrcFilePath));
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public void PositiveProcessImagesFileTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = correctSrcFilePath, DstRootPath = dstFilePath, Mode = Mode.Copy };
            Assert.IsTrue(imgMigrator.ProcessImages());
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public void UnpositiveProcessImageTest(string incorrectSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = incorrectSrcFilePath, DstRootPath = dstFilePath, Mode = Mode.Copy };
            Assert.IsFalse(imgMigrator.ProcessImages());
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public void PositiveProcessDirectoryTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = correctSrcFilePath, DstRootPath = dstFilePath, Mode = Mode.Copy };
            Assert.IsTrue(imgMigrator.ProcessDirectory(correctSrcFilePath));

            // TODO check all expected files exists
            Assert.AreEqual(VALID_IMAGES_COUNT, Directory.EnumerateFiles(dstFilePath, "*.jpg", SearchOption.AllDirectories).Count());
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void UnpositiveProcessDirectoryIncorrectSrcPathTest(string incorrectSrcFile, string dstFilePath)
        {
            var imgMigartor = new ImgMigrator() { SrcPath = incorrectSrcFile, DstRootPath = dstFilePath, Mode = Mode.Copy };
            imgMigartor.ProcessDirectory(incorrectSrcFile);

            Assert.AreEqual(0, Directory.EnumerateFiles(dstFilePath, "*.*", SearchOption.AllDirectories).Count());
        }

        #endregion
    }
}