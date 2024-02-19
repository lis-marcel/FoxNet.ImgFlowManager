using ExifLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img
{
    public static class StringUtils
    {
        public static string ReplaceSpecialCharacters(this string input)
        {
            string normalizedString = input.Normalize(NormalizationForm.FormKD);

            StringBuilder result = new();
            foreach (char c in normalizedString)
            {
                if (c == 'ł' || c == 'Ł')
                {
                    result.Append('l');
                }
                else
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (category != UnicodeCategory.NonSpacingMark)
                    {
                        result.Append(c);
                    }
                }
            }

            return result.ToString();
        }
        
        public static string RemoveTextSpaces(this string imgName)
        {
            var words = imgName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var processedFileName = string.Join("", words);

            return processedFileName;
        }

    }
}
