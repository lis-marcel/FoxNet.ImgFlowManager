using FoxSky.Img.FileProcessors;
using FoxSky.Img.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        private GeolocationService _geolocationService;
        private FileHandler _fileHandler;
        private LocationCache _locationCache;

        [TestInitialize]
        public void InitTest()
        {
            // Arrange - Setup for all tests
            if (Directory.Exists(DST_FILE_PATH))
                Directory.Delete(DST_FILE_PATH, true);

            _locationCache = new LocationCache();
            _geolocationService = new GeolocationService(_locationCache);
            _fileHandler = new FileHandler(_geolocationService);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public async Task Test_ProcessImageFile_Fail(string correctSrcFile, string incorrectDstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = correctSrcFile,
                DstRootPath = incorrectDstFilePath
            };

            // Act
            var result = await _fileHandler.ProcessImageFile(correctSrcFile, imageProcessor);

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Error, result);
        }

        [TestMethod]
        [DataRow(FILE_1, DST_FILE_PATH)]
        public async Task Test_ProcessImageFile_Correct(string correctSrcFilePath, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                DstRootPath = dstFilePath,
                PicsOwnerSurname = "Test"
            };

            // Act
            var result1 = await _fileHandler.ProcessImageFile(correctSrcFilePath, imageProcessor);
            var result2 = await _fileHandler.ProcessImageFile(correctSrcFilePath, imageProcessor);
            var result3 = await _fileHandler.ProcessImageFile(correctSrcFilePath, imageProcessor);

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result1);
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result2);
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result3);

            Assert.IsTrue(File.Exists(Path.Combine(dstFilePath, @"2019\Test_2019-02-18 16-11-33.jpg")),
                "Expected file should exist");

            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\Test_2019-02-18 16-11-33_0.jpg")),
                "Duplicate file _0 should not exist");
            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\Test_2019-02-18 16-11-33_1.jpg")),
                "Duplicate file _1 should not exist");
            Assert.IsFalse(File.Exists(Path.Combine(dstFilePath, @"2019\Test_2019-02-18 16-11-33_2.jpg")),
                "Duplicate file _2 should not exist");
        }

        [TestMethod]
        public async Task Test_ProcessImageFilePlusByte_Correct()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                DstRootPath = DST_FILE_PATH,
                PicsOwnerSurname = "Test"
            };

            // Act
            var result1 = await _fileHandler.ProcessImageFile(FILE_1, imageProcessor);
            var result2 = await _fileHandler.ProcessImageFile(FILE_1_PLUS, imageProcessor);

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result1);
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result2);

            Assert.IsTrue(File.Exists(Path.Combine(DST_FILE_PATH, @"2019\Test_2019-02-18 16-11-33.jpg")),
                "Original file should exist");
            Assert.IsTrue(File.Exists(Path.Combine(DST_FILE_PATH, @"2019\Test_2019-02-18 16-11-33_1.jpg")),
                "Different content duplicate should exist with _1 suffix");

            Assert.IsFalse(File.Exists(Path.Combine(DST_FILE_PATH, @"2019\Test_2019-02-18 16-11-33_0.jpg")),
                "File with _0 suffix should not exist");
            Assert.IsFalse(File.Exists(Path.Combine(DST_FILE_PATH, @"2019\Test_2019-02-18 16-11-33_2.jpg")),
                "File with _2 suffix should not exist");
        }

        [TestMethod]
        [DataRow(TXT_FILE, DST_FILE_PATH)]
        public async Task Test_WrongExtension_Fail(string incorrectSrcFilePath, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = incorrectSrcFilePath,
                DstRootPath = dstFilePath
            };

            // Act
            var result = await _fileHandler.ProcessImageFile(incorrectSrcFilePath, imageProcessor);

            // Assert
            Assert.AreEqual(result, EnviromentExitCodes.ExitCodes.Error);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public async Task Test_ProcessImagesFile_Correct(string correctSrcFilePath, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = correctSrcFilePath,
                DstRootPath = dstFilePath,
                PicsOwnerSurname = "Test"
            };

            // Act
            var result = await imageProcessor.Run();

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result);
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public async Task Test_ProcessImage_Fail(string incorrectSrcFilePath, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = incorrectSrcFilePath,
                DstRootPath = dstFilePath
            };

            // Act
            var result = await imageProcessor.Run();

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Error, result);
        }

        [TestMethod]
        [DataRow(CORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        public async Task Test_ProcessDirectory_Correct(string correctSrcFilePath, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = correctSrcFilePath,
                DstRootPath = dstFilePath,
                PicsOwnerSurname = "Test"
            };

            // Act
            var result = await _fileHandler.ProcessDirectory(correctSrcFilePath, imageProcessor);

            // Assert
            Assert.AreEqual(EnviromentExitCodes.ExitCodes.Succcess, result);
            Assert.AreEqual(VALID_IMAGES_COUNT,
                Directory.EnumerateFiles(dstFilePath, "*.jpg", SearchOption.AllDirectories).Count(),
                "Expected number of processed files should match");
        }

        [TestMethod]
        [DataRow(INCORRECT_INPUT_FILE_PATH, DST_FILE_PATH)]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public async Task Test_ProcessDirectoryIncorrectSrcPath_Fail(string incorrectSrcFile, string dstFilePath)
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = incorrectSrcFile,
                DstRootPath = dstFilePath
            };

            // Act
            await _fileHandler.ProcessDirectory(incorrectSrcFile, imageProcessor);

            // Assert
            // Exception is expected, handled by ExpectedException attribute
            Assert.AreEqual(0,
                Directory.EnumerateFiles(dstFilePath, "*.*", SearchOption.AllDirectories).Count(),
                "No files should be processed with invalid source directory");
        }

        // Geolocation tests are commented out as they require external service
        //[TestMethod]
        //public async Task GeolocationFlag_When_Enabled_Should_Include_Location_In_Filename()
        //{
        //    // Arrange
        //    var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, Mode.Copy)
        //    {
        //        DstRootPath = DST_FILE_PATH,
        //        PicsOwnerSurname = "Test",
        //        GeolocationFlag = true,
        //        UserEmail = "test@example.com",
        //        Radius = "100"
        //    };

        //    // Act
        //    var result = await _fileHandler.ProcessImageFile(FILE_1, imageProcessor);

        //    // Assert
        //    Assert.IsTrue(result, "Processing with geolocation flag should succeed");
        //}

        //[TestMethod]
        //public async Task GeolocationFlag_When_Disabled_Should_Not_Include_Location_In_Filename()
        //{
        //    // Arrange
        //    var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, Mode.Copy)
        //    {
        //        DstRootPath = DST_FILE_PATH,
        //        PicsOwnerSurname = "Test",
        //        GeolocationFlag = false
        //    };

        //    // Act
        //    var result = await _fileHandler.ProcessImageFile(FILE_1, imageProcessor);

        //    // Assert
        //    Assert.IsTrue(result, "Processing without geolocation flag should succeed");
        //    Assert.IsTrue(File.Exists(Path.Combine(DST_FILE_PATH, @"2019\Test_2019-02-18 16-11-33.jpg")),
        //        "Filename should not include location data");
        //}
    }
}
