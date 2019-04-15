using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class Constants
    {
        public static List<string> CONSTEXT = new List<string> { ".cs", ".ts", ".config", " *.jpg", "*.gif", "*.cpp", "*.c", "*.htm", "*.html", "*.xsp", "*.asp", "*.xml", "*.h", "*.asmx", "*.asp", "*.atp", "*.bmp", "*.dib", "*.config", "*.sln", "*.txt" };
        public static string CONSTFILTER = @"solution files (*.sln)|*.sln";

        public static string ERRKEYWORDREQUIRED = @"Please enter keyword's that need to search !";
    }
}
