using FoxSky.Img.FileProcessors;
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

        SetupEnvironment(args, imageProcessor!);

        var resSuccess = Task.Run(async () => await imageProcessor!.ProcessImages()).GetAwaiter().GetResult();
        Environment.ExitCode = resSuccess ? 0 : 1;
    }

    private static void SetupEnvironment(string[] args, ImageProcessor imageProcessor)
    {
        if (imageProcessor == null)
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

        var mode = ModeExtenstions.GetModeString(args[3]);
        if (mode == null)
        {
            Logger.LogError($"Invalid mode: {mode}");
            Environment.ExitCode = 1;
            return;
        }

        imageProcessor.GeolocationFlag = args.Contains("-g");

        if (imageProcessor.GeolocationFlag && args.Length < 6)
        {
            Logger.LogError("Invalid geolocation params.");
            Environment.ExitCode = 1;
            return;
        }

        imageProcessor.UserEmail = args.Length > 4 ? args[4] : null;
        imageProcessor.Radius = args.Length > 5 ? args[5] : null;
    }
}