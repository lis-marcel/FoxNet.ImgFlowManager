using FoxSky.Img.Service;
using FoxSky.Img.Utilities;

namespace FoxSky.Img.FileProcessors
{
    public class ImageProcessor
    {
        private readonly FileHandler fileHandler;
        private readonly GeolocationService geolocationService;

        #region Public Fields
        public string? OwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public bool GeolocationFlag { get; set; }
        public string? UserEmail { get; set; }
        public string? Radius { get; set; }
        public OperationMode Mode { get; set; }
        #endregion

        public ImageProcessor(FileHandler fileHandler, GeolocationService geolocationService, OperationMode mode)
        {
            this.fileHandler = fileHandler;
            this.geolocationService = geolocationService;
            Mode = mode;
        }

        public async Task<int> Run()
        {
            if (!Directory.Exists(SrcPath) && !Directory.Exists(DstRootPath)) 
            {
                Logger.LogError("Given src or dst path doesn't exist!");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }

            return await fileHandler.ProcessDirectory(this);
        }
    }
}
