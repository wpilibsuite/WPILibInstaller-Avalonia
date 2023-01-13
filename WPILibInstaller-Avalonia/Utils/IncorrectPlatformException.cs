using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    public class IncorrectPlatformException : Exception
    {

        public IncorrectPlatformException(string requested, string current) : base($"Installer {requested} needed for current system.\nCurrent installer is {current}.")
        {

        }
    }
}
