using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img.Utilities
{
    public class TextUtils
    {
        public static string ReplaceSpecialCharacters(string input)
        {
            Dictionary<char, char> characterReplacements = new()
            {
                {'Ł', 'L'},
                {'ł', 'l'}
            };

            StringBuilder result = new(input.Length);

            foreach (char c in input)
            {
                if (characterReplacements.TryGetValue(c, out char replacement))
                {
                    result.Append(replacement);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        public static string RemoveSpaces(string s)
        {
            return !string.IsNullOrEmpty(s) ? s.Replace(" ", "") : s;
        }
    }
}
