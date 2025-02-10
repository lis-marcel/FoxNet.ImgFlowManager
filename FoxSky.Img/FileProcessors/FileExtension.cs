using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img.FileProcessors
{
    public enum FileExtension
    {
        JEPG,
        JPG,
        HEIC,
        PNG,
    }

    public static class FileExtensionExtenstions
    {
        public static string ToExtension(this FileExtension fileExtension)
        {
            return fileExtension switch
            {
                FileExtension.JEPG => ".jpeg",
                FileExtension.JPG => ".jpg",
                FileExtension.HEIC => ".heic",
                FileExtension.PNG => ".png",
                _ => throw new ArgumentException("Unsupported file extension"),
            };
        }

        public static IEnumerable<string> GetExtensions()
        {
            return Enum.GetValues(typeof(FileExtension))
                .Cast<FileExtension>()
                .Select(ext => ext.ToExtension());
        }
    }
}