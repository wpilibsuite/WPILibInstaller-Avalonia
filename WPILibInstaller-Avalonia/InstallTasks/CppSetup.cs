using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class CppSetupTask : InstallTask
    {

        private readonly IConfigurationProvider configurationProvider;

        public CppSetupTask (
            IConfigurationProvider pConfigurationProvider
        )
        {
            configurationProvider = pConfigurationProvider;
        }

        public override async Task Execute(CancellationToken token)
        {
            Text = "Configuring C++";
            Progress = 50;

            await Task.Yield();
        }
    }
}

