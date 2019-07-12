using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dht.Sharp
{
    public static class NativeTimerFunctions
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        extern public static long GetTickCount64();

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        extern public static bool QueryPerformanceFrequency(out long frequency);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        extern public static bool QueryPerformanceCounter(out long count);

        public static long PerformanceFrequency { get; } = QueryPerformanceFrequency();

        public static decimal PerformanceTicksPerMillisec { get; } = (decimal)PerformanceFrequency / 1000;

        public static decimal PerformanceTicksPerMicrosec { get; } = (decimal)PerformanceFrequency / 1000000;

        private static long QueryPerformanceFrequency()
        {
            if (!QueryPerformanceFrequency(out var freq))
            {
                throw new Exception("QueryPerformanceFrequency failed", new Win32Exception());
            }

            return freq;
        }

        public static long PerformanceCounter
        {
            get
            {
                if (!QueryPerformanceCounter(out var count))
                {
                    throw new Exception("QueryPerformanceCounter failed", new Win32Exception());
                }

                return count;
            }
        }

        public static double PerformanceTimeSeconds => (double)PerformanceCounter / PerformanceFrequency;

        public static decimal PerformanceTimeMillisec => PerformanceCounter / PerformanceTicksPerMillisec;

        public static decimal PerformanceTimeMicrosec => PerformanceCounter / PerformanceTicksPerMicrosec;

        public static TimeSpan PerformanceTimeSpan => TimeSpan.FromSeconds(PerformanceTimeSeconds);

        public static void SpinWaitMicroseconds(long microsec)
        {
            var perf_count = PerformanceCounter +
                (long)Math.Ceiling(PerformanceTicksPerMicrosec * microsec);

            while (PerformanceCounter < perf_count)
            {
            }
        }

    }
}
