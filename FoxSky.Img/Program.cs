using FoxSky.Img.Processors;
using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ImageProcessor>()
            .AddSingleton<FileHandler>()
            .AddSingleton<GeolocationService>()
            .AddSingleton<LocationCache>()
            .BuildServiceProvider();

        var imageProcessor = serviceProvider.GetService<ImageProcessor>();

        if (imageProcessor == null ) 
        { 
            Logger.LogError("Service build failed."); 
            Environment.ExitCode = 1; 
            return; 
        }

        if (args.Length < 3)
        {
            Logger.LogError("Invalid base params.");
            Environment.ExitCode = 1;
            return;
        }

        imageProcessor.PicsOwnerSurname = args[0];
        imageProcessor.SrcPath = args[1];
        imageProcessor.DstRootPath = args[2];

        imageProcessor.AddGeolocationFlag = args.Contains("-g");

        if (imageProcessor.AddGeolocationFlag && args.Length < 5)
        {
            Logger.LogError("Invalid geolocation params.");
            Environment.ExitCode = 1;
            return;
        }

        imageProcessor.UserEmail = args.Length > 4 ? args[4] : null;
        imageProcessor.Radius = args.Length > 5 ? args[5] : null;

        var resSuccess = Task.Run(async () => await imageProcessor.ProcessImages()).GetAwaiter().GetResult();
        Environment.ExitCode = resSuccess ? 0 : 1;
    }
}