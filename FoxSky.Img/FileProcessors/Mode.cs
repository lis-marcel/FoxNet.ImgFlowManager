using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxSky.Img.FileProcessors
{
    public enum Mode
    {
        Copy = 1,
        Move = 2,
    }

    public static class ModeExtenstions
    {
        private static Dictionary<string, Mode> modesMap = new()
        {
            { "1", Mode.Copy },
            { "2", Mode.Move }
        };

        public static Mode? GetModeString(string modeNumber)
        {
            Mode? mode = null;

            foreach (var item in modesMap)
            {
                if (item.Value.ToString() == modeNumber)
                {
                    mode = item.Value;
                }
            }

            return mode;
        }
    }
}
