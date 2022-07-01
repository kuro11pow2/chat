using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utility
{
    public static class TimerTokenSource
    {
        public static CancellationTokenSource GetTimer(int millisecond)
        {
            CancellationTokenSource timerTokenSource = new();
            timerTokenSource.CancelAfter(millisecond);
            return timerTokenSource;
        }
    }
}
