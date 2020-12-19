using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    public class IncorrectPlatformException : Exception
    {

        public IncorrectPlatformException(string platform) : base($"Installer {platform} needed for current system")
        {

        }
    }
}
