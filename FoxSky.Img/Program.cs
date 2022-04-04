using FoxSky.Img;
using System;

class Program
{
    static void Main(string[] args)
    {
        var srcPath = @"C:\temp\imgMigrator\src";
        var dstPath = @"C:\temp\imgMigrator\dst";

        var imgMigrator = new ImgMigrator() { SrcPath = srcPath, DstRootPath = dstPath};

        imgMigrator.GetFilesFromSrc(srcPath);
    }
}