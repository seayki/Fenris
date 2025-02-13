using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris
{
    public class ProcessTerminator
    {
        public ProcessTerminator()
        {
            throw new NotImplementedException();
        }

        public List<string> RetrieveProcessesToTerminate()
        {
             throw new NotImplementedException();
        }

        public void TerminateProcessBackgroundService()
        {
            while (true)
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("BackgroundService");
                foreach (var processToTerminate in processes)
                {
                    processToTerminate.Kill();
                }
            }
        }
    }
}
