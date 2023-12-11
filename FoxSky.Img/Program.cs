using FoxSky.Img;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        var resSuccess = false;
        var stopwatch = new Stopwatch();

        stopwatch.Start();

        if (args.Length < 3)
        {
            ImgMigrator.LogError("Invalid params.");
        }
        else
        {

            var imgMigrator = new ImgMigrator() { PicsOwnerSurname = args[0], SrcPath = args[1], DstRootPath = args[2] };
            resSuccess = imgMigrator.ProcessImages();
        }

        stopwatch.Stop();

        var elapsedTime = stopwatch.Elapsed;
        Console.WriteLine($"Elapsed time: {elapsedTime}");

        Environment.ExitCode = resSuccess ? 0 : 1;
    }
}