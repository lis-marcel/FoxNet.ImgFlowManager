using Microsoft.VisualStudio.TestTools.UnitTesting;
using FoxSky.Img;
using System;
using System.IO;
using System.Collections.Generic;

namespace ImgMigratorTests
{
    [TestClass]
    public class ImgMigratorTests
    {
        #region Consts
        const string CORRECT_INPUT_FILE_PATH = @".\Input";
        const string INCORRECT_INPUT_FILE_PATH = @".\xyz";
        const string CORRECT_DST_FILE_PATH = @".\ZYX";
        const string FILE_1 = @".\Input\compare\file1.jpg";
        const string TXT_FILE = @".\Input\images.txt";
        #endregion

        #region Test methods

        //TODO: program copies to unexisting file!
        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, CORRECT_DST_FILE_PATH)]
        public void UnpositiveProcessProcessImageFileTest(string correctSrcFile, string incorrectDstFilePath)
        {
            var imgMigartor = new ImgMigrator() { SrcPath = correctSrcFile, DstRootPath = incorrectDstFilePath, Mode = Mode.Copy };
            imgMigartor.ProcessImageFile(correctSrcFile);
        }

        [TestMethod]
        [DataRow(FILE_1, CORRECT_DST_FILE_PATH)]
        public void PositiveProcessImageFileTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = correctSrcFilePath, DstRootPath = dstFilePath };
            var res = imgMigrator.ProcessImageFile(correctSrcFilePath);

            Assert.AreEqual(false, res);
        }

        [TestMethod]
        [DataRow(TXT_FILE, CORRECT_DST_FILE_PATH)]
        public void WrongExtensionTest(string incorrectSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = incorrectSrcFilePath, DstRootPath = dstFilePath };
            var res = imgMigrator.ProcessImageFile(incorrectSrcFilePath);

            Assert.AreEqual(false, res);
        }

        //TODO
        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, TXT_FILE)]
        public void WrongDstFileExtensionTest(string incorrectSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = incorrectSrcFilePath, DstRootPath = dstFilePath };
            var res = imgMigrator.ProcessImageFile(incorrectSrcFilePath);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, CORRECT_DST_FILE_PATH)]
        public void PositiveProcessImagesFileTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = correctSrcFilePath, DstRootPath = dstFilePath };
            var res = imgMigrator.ProcessImages();

            Assert.AreEqual(true, res);
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, CORRECT_DST_FILE_PATH)]
        public void UnpositiveProcessImageTest(string incorrectSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = incorrectSrcFilePath, DstRootPath = dstFilePath };
            var res = imgMigrator.ProcessImages();

            Assert.AreEqual(false, res);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, CORRECT_DST_FILE_PATH)]
        public void PositiveProcessDirectoryTest(string correctSrcFilePath, string dstFilePath)
        {
            var imgMigrator = new ImgMigrator() { SrcPath = correctSrcFilePath, DstRootPath = dstFilePath};
            var res = imgMigrator.ProcessDirectory(correctSrcFilePath);

            Assert.AreEqual(true, res);
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, CORRECT_DST_FILE_PATH)]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void UnpositiveProcessDirectoryIncorrectSrcPathTest(string incorrectSrcFile, string dstFilePath)
        {
            var imgMigartor = new ImgMigrator() { SrcPath = incorrectSrcFile, DstRootPath = dstFilePath };
            imgMigartor.ProcessDirectory(incorrectSrcFile);
        }

        #endregion
    }
}