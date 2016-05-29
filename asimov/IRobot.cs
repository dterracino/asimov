using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asimov
{
    interface IRobot
    {
        Task RunAsync();
        void Stop();
    }
}
