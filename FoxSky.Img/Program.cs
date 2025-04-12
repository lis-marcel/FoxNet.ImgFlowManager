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
            .WithParsed(options => cmdOptions = options);

        var paramsValidation = cmdOptions.ValidateAndPrompt();

        // Exit application if no params were provided
        if (paramsValidation != (int)EnviromentExitCodes.ExitCodes.Succcess)
        {
            Environment.Exit((int)EnviromentExitCodes.ExitCodes.Error);
        }

        var serviceProvider = SetupServices(cmdOptions);

        var imageProcessor = serviceProvider.GetService<ImageProcessor>();

        // Set paths from command options
        imageProcessor!.SrcPath = cmdOptions.SrcPath;
        imageProcessor.DstRootPath = cmdOptions.DstPath;

        PrintSetupSummary(cmdOptions);

        var exitCode = Task.Run(async () => await imageProcessor!.Run()).GetAwaiter().GetResult();

        ExitWithCode(exitCode);
    }

    private static ServiceProvider SetupServices(CmdOptions cmdOptions)
    {
        return new ServiceCollection()
            .AddSingleton<FileHandler>()
            .AddSingleton<GeolocationService>()
            .AddSingleton<LocationCache>()
            .AddSingleton<ImageProcessor>(provider => new ImageProcessor(
                provider.GetRequiredService<FileHandler>(),
                provider.GetRequiredService<GeolocationService>(),
                cmdOptions.Mode == OperationMode.Copy ? OperationMode.Copy : OperationMode.Move
            ))
            .BuildServiceProvider();
    }

    private static void PrintSetupSummary(CmdOptions cmdOptions)
    {
        Logger.LogInfo($"Processing images from: {cmdOptions.SrcPath}");
        Logger.LogInfo($"Destination path: {cmdOptions.DstPath}");
        Logger.LogInfo($"Operation mode: {cmdOptions.Mode}");
    }

    private static void ExitWithCode(int exitCode)
    {
        if (exitCode == (int)EnviromentExitCodes.ExitCodes.Succcess)
        {
            Logger.LogSuccess("Processing completed successfully.");
            Environment.Exit((int)EnviromentExitCodes.ExitCodes.Succcess);
        }
        else if (exitCode == (int)EnviromentExitCodes.ExitCodes.Warninig)
        {
            Logger.LogError("An error occurred during processing.");
            Environment.Exit((int)EnviromentExitCodes.ExitCodes.Warninig);
        }
        else
        {
            Logger.LogError("An error occurred during processing.");
            Environment.Exit((int)EnviromentExitCodes.ExitCodes.Error);
        }
    }
}
