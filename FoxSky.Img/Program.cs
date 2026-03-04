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
            .AddSingleton<ImageProcessor>(provider => {
                var processor = new ImageProcessor(
                    provider.GetRequiredService<FileHandler>(),
                    provider.GetRequiredService<GeolocationService>(),
                    cmdOptions.Mode == OperationMode.Copy ? OperationMode.Copy : OperationMode.Move
                )
                {
                    // Set all required properties
                    SrcPath = cmdOptions.SrcPath,
                    DstRootPath = cmdOptions.DstPath,
                    OwnerSurname = cmdOptions.OwnerSurname,

                    // TODO: implement including geolocation
                    GeolocationFlag = cmdOptions.GeolocationFlag, // Add this flag to CmdOptions
                    UserEmail = cmdOptions.UserEmail,             // Add this to CmdOptions if needed
                    Radius = cmdOptions.Radius                   // Add this to CmdOptions if needed
                };

                return processor;
            })
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
