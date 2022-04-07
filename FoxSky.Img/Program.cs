using FoxSky.Img;
using System;

class Program
{
    static void Main(string[] args)
    {
        var resSuccess = false;

        if (args.Length < 2)
        {
            ImgMigrator.LogError("Invalid params.");
        }
        else
        {
            var imgMigrator = new ImgMigrator() { SrcPath = args[0], DstRootPath = args[1] };
            resSuccess = imgMigrator.ProcessImages();
        }

        Environment.ExitCode = resSuccess ? 0 : 1;
    }
}