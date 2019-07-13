using System;
using System.Runtime.InteropServices;

namespace LTRLib.LTRGeneric
{
    public static class NativeTimerFunctions
    {
        /// <summary>
        /// Low accuracy timer.
        /// </summary>
        /// <returns>Returns number of ms since boot.</returns>
        [DllImport("kernel32.dll")]
        extern public static long GetTickCount64();

        [DllImport("kernel32.dll")]
        extern private static void QueryPerformanceFrequency(out long frequency);

        [DllImport("kernel32.dll")]
        extern private static void QueryPerformanceCounter(out long count);

        public static long PerformanceCountsPerSecond { get; }

        public static long TicksPerMicrosecond { get; } =
            TimeSpan.FromSeconds(1).Ticks / 1000000;

        private static long performance_counts_per_ticks_multiplier;
        private static long performance_counts_per_ticks_divisor;

        static NativeTimerFunctions()
        {
            QueryPerformanceFrequency(out var freq);

            PerformanceCountsPerSecond = freq;

            performance_counts_per_ticks_multiplier = PerformanceCountsPerSecond;
            performance_counts_per_ticks_divisor = TimeSpan.FromSeconds(1).Ticks;

            while ((performance_counts_per_ticks_multiplier & 1) == 0 &&
                (performance_counts_per_ticks_divisor & 1) == 0)
            {
                performance_counts_per_ticks_multiplier >>= 1;
                performance_counts_per_ticks_divisor >>= 1;
            }
        }

        public static long PerformanceCountsFromMicroseconds(long microsec) =>
            PerformanceCountsFromTicks(checked(microsec * TicksPerMicrosecond));

        public static long PerformanceCountsFromTicks(long ticks)
        {
            var prod = checked(ticks * performance_counts_per_ticks_multiplier);
            var value = prod / performance_counts_per_ticks_divisor;

            if ((prod % performance_counts_per_ticks_divisor) != 0)
            {
                ++value;
            }

            return value;
        }

        /// <summary>
        /// High accuracy timer.
        /// </summary>
        /// <value>Number of performance timer counts since boot.
        /// 
        /// Number of performance timer counts per second can be found in <see cref="PerformanceCountsPerSecond"> property.</see>"/></value>
        public static long PerformanceCounter
        {
            get
            {
                QueryPerformanceCounter(out var count);
                return count;
            }
        }

        public static TimeSpan PerformanceCountsToTimeSpan(long count) =>
            TimeSpan.FromTicks(checked(count * performance_counts_per_ticks_divisor / performance_counts_per_ticks_multiplier));

        /// <summary>
        /// Waits specified number of performance timer counts by spinning in a tight loop until at least timer counts have passed.
        /// 
        /// Number of performance timer counts per second can be found in <see cref="PerformanceCountsPerSecond"> property.</see>"/>
        /// </summary>
        /// <param name="perf_count">Number of performance timer counts to wait.</param>
        public static void SpinWaitPerformanceCounts(long perf_count)
        {
            perf_count += PerformanceCounter;

            while (PerformanceCounter < perf_count)
            {
            }
        }

    }
}
