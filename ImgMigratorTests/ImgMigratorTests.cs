using FoxSky.Img.FileProcessors;
using FoxSky.Img.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ImgMigratorTests
{
    [TestClass]
    public class ImgMigratorTests
    {
        // TODO: Refactor to use relative paths
        #region Consts
        private readonly string CORRECT_INPUT_FILE_PATH = @"C:\temp\in";
        private readonly string INCORRECT_INPUT_FILE_PATH = @"no existing path";
        private readonly string DST_FILE_PATH = @"C:\temp\out";
        private readonly string FILE_1 = @"C:\temp\in\compare\file1.jpg";
        private readonly string FILE_1_PLUS = @"C:\temp\in\compare\file1_plus_one_byte.jpg";
        private readonly string TXT_FILES = @"C:\temp\in\txtFiles\images.txt";
        private readonly int VALID_IMAGES_COUNT = 6;
        #endregion

        private GeolocationService _geolocationService;
        private FileHandler _fileHandler;
        private LocationCache _locationCache;

        [TestInitialize]
        public void InitTest()
        {
            _locationCache = new LocationCache();
            _geolocationService = new GeolocationService(_locationCache);
            _fileHandler = new FileHandler(_geolocationService);
        }

        [TestMethod]
        public async Task Test_ProcessImageFile_Fail()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = CORRECT_INPUT_FILE_PATH,
                DstRootPath = INCORRECT_INPUT_FILE_PATH,
                Mode = OperationMode.Copy,
            };

            // Act
            var result = await _fileHandler.ProcessImageFile(CORRECT_INPUT_FILE_PATH, imageProcessor);

            // Assert
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Error, result);
        }

        [TestMethod]
        public async Task Test_WrongExtension_Fail()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = TXT_FILES,
                DstRootPath = DST_FILE_PATH
            };

            // Act
            var result = await _fileHandler.ProcessImageFile(TXT_FILES, imageProcessor);

            // Assert
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Error, result);
        }

        [TestMethod]
        public async Task Test_ProcessImagesFile_Correct()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = CORRECT_INPUT_FILE_PATH,
                DstRootPath = DST_FILE_PATH,
                PicsOwnerSurname = "Test"
            };

            // Act
            var result = await imageProcessor.Run();

            // Assert
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Succcess, result);
        }

        [TestMethod]
        public async Task Test_ProcessImage_Fail()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = INCORRECT_INPUT_FILE_PATH,
                DstRootPath = DST_FILE_PATH
            };

            // Act
            var result = await imageProcessor.Run();

            // Assert
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Error, result);
        }

        [TestMethod]
        public async Task Test_ProcessDirectory_Correct()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = CORRECT_INPUT_FILE_PATH,
                DstRootPath = DST_FILE_PATH,
                PicsOwnerSurname = "Test",
                Mode = OperationMode.Copy,
            };

            // Act
            var result = await _fileHandler.ProcessDirectory(imageProcessor);

            // Assert
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Succcess, result);
        }

        [TestMethod]
        public async Task Test_ProcessDirectoryIncorrectSrcPath_Fail()
        {
            // Arrange
            var imageProcessor = new ImageProcessor(_fileHandler, _geolocationService, OperationMode.Copy)
            {
                SrcPath = INCORRECT_INPUT_FILE_PATH,
                DstRootPath = DST_FILE_PATH,
                PicsOwnerSurname = "Test",
                Mode = OperationMode.Copy,
            };

            // Act
            var result = await _fileHandler.ProcessDirectory(imageProcessor);

            // Assert
            // Exception is expected, handled by ExpectedException attribute
            Assert.AreEqual((int)EnviromentExitCodes.ExitCodes.Error, result);
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
