using CommandLine;
using FoxSky.Img.FileProcessors;
using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        var cmdOptions = new CmdOptions();

        Parser.Default.ParseArguments<CmdOptions>(args)
            .WithParsed(options => cmdOptions = options)
            .WithNotParsed(errors => Logger.LogError("Invalid arguments provided."));

        if (cmdOptions.ValidateAndPrompt() == (int)EnviromentExitCodes.ExitCodes.Error)
        {
            Environment.ExitCode = (int)EnviromentExitCodes.ExitCodes.Error;
        }

        var serviceProvider = new ServiceCollection()
            .AddSingleton<FileHandler>()
            .AddSingleton<GeolocationService>()
            .AddSingleton<LocationCache>()
            .AddSingleton<ImageProcessor>(provider => new ImageProcessor(
                provider.GetRequiredService<FileHandler>(),
                provider.GetRequiredService<GeolocationService>(),
                cmdOptions.Mode == OperationMode.Copy ? OperationMode.Copy : OperationMode.Move
            ))
            .BuildServiceProvider();

        var imageProcessor = serviceProvider.GetService<ImageProcessor>();

        // Set paths from command options
        imageProcessor!.SrcPath = cmdOptions.SrcPath;
        imageProcessor.DstRootPath = cmdOptions.DstPath;

        Logger.LogInfo($"Processing images from: {cmdOptions.SrcPath}");
        Logger.LogInfo($"Destination path: {cmdOptions.DstPath}");
        Logger.LogInfo($"Operation mode: {cmdOptions.Mode}");

        Task.Run(async () => await imageProcessor!.Run()).GetAwaiter().GetResult();
    }
}
