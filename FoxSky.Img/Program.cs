using FoxSky.Img.FileProcessors;
using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<FileHandler>()
            .AddSingleton<GeolocationService>()
            .AddSingleton<LocationCache>()
            .AddSingleton<ImageProcessor>(provider => new ImageProcessor(
                provider.GetRequiredService<FileHandler>(),
                provider.GetRequiredService<GeolocationService>(),
                Mode.Copy
            ))
            .BuildServiceProvider();

        var imageProcessor = serviceProvider.GetService<ImageProcessor>();

        if (!SetupEnvironment(args, imageProcessor!))
        {
            return;
        }

        var resSuccess = Task.Run(async () => await imageProcessor!.ProcessImages()).GetAwaiter().GetResult();
        Environment.ExitCode = resSuccess ? 0 : 1;
    }

    private static bool SetupEnvironment(string[] args, ImageProcessor imageProcessor)
    {
        if (imageProcessor == null)
        {
            Logger.LogError("Service build failed.");
            Environment.ExitCode = 1;
            return false;
        }

        if (args.Length < 4) // Changed from 3 to 4 since mode is required
        {
            Logger.LogError("Invalid base params. Required: PicsOwnerSurname SrcPath DstRootPath Mode");
            Environment.ExitCode = 1;
            return false;
        }

        imageProcessor.PicsOwnerSurname = args[0];
        imageProcessor.SrcPath = args[1];
        imageProcessor.DstRootPath = args[2];

        if (Enum.TryParse<Mode>(args[3], true, out var parsedMode))
        {
            imageProcessor.Mode = parsedMode;
        }
        else
        {
            var mode = ModeExtenstions.GetModeString(args[3]);
            if (mode == null)
            {
                Logger.LogError($"Invalid mode: {args[3]}. Valid modes are: Copy, Move");
                Environment.ExitCode = 1;
                return false;
            }
            imageProcessor.Mode = mode.Value;
        }

        imageProcessor.GeolocationFlag = args.Contains("-g");

        if (imageProcessor.GeolocationFlag && args.Length < 6)
        {
            Logger.LogError("Invalid geolocation params. Required with -g: UserEmail Radius");
            Environment.ExitCode = 1;
            return false;
        }

        imageProcessor.UserEmail = args.Length > 4 ? args[4] : null;
        imageProcessor.Radius = args.Length > 5 ? args[5] : null;

        return true;
    }
}