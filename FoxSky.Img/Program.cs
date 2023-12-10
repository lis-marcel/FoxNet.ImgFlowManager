using FoxSky.Img;

class Program
{
    static void Main(string[] args)
    {
        var resSuccess = false;

        if (args.Length < 3)
        {
            ImgMigrator.LogError("Invalid params.");
        }
        else
        {
            var imgMigrator = new ImgMigrator() { PicsOwnerSurname = args[0], SrcPath = args[1], DstRootPath = args[2] };
            resSuccess = imgMigrator.ProcessImages();
        }

        Environment.ExitCode = resSuccess ? 0 : 1;
    }
}