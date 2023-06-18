using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare
{
    internal class BackgroundManager
    {

        public static void WakeCpu()
        {
#if ANDROID
          MainActivity.WakeCpu();
#endif
        }
        public static void SleepCpu()
        {
#if ANDROID
          MainActivity.SleepCpu();
#endif
        }
    }
}
