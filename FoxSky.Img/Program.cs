using FoxSky.Img;
using System;

class Program
{
    static void Main(string[] args)
    {
        var resSuccess = false;

        if (args.Length < 4)
        {
            ImgMigrator.LogError("Invalid params.");
        }
        else
        {
            var imgMigrator = new ImgMigrator() { PicsOwnerSurname = args[0], SrcPath = args[1], DstRootPath = args[2], Distance = args[3] };
            resSuccess = Task.Run(async () => await imgMigrator.ProcessImages()).GetAwaiter().GetResult();
        }

        Environment.ExitCode = resSuccess ? 0 : 1;
    }
}