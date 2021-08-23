//
// Copyright © 2005-2019 Olof Lagerkvist
//
// This is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dht.Sharp Solution is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Dht.Sharp Solution. If not, see http://www.gnu.org/licenses/.
//

using System;
using System.Runtime.InteropServices;

namespace LTRLib.LTRGeneric
{
    using Extensions;

    /// <summary>
    /// Static methods and properties for accessing high-performance timer values.
    /// </summary>
    internal static class PerformanceTimers
    {
        /// <summary>
        /// Low accuracy timer.
        /// </summary>
        /// <returns>Returns number of ms since boot. Updates about every 15-20 ms.</returns>
        [DllImport("kernel32.dll")]
        extern public static long GetTickCount64();

        /// <summary>
        /// Frequency of performance counter. This is the number the performance counter will
        /// count in a second. This value depends on hardware and is usually somewhere around
        /// 10-20 MHz.
        /// </summary>
        public static long PerformanceCountsPerSecond { get; }

        /// <summary>
        /// Returns current value of the performance counter. This is a high accuracy timer.
        /// </summary>
        /// <value>Number of performance timer counts since boot.
        /// 
        /// Number of performance timer counts per second can be found in <see cref="PerformanceCountsPerSecond"> property.</see>"/></value>
        public static long PerformanceCounterValue
        {
            get
            {
                QueryPerformanceCounter(out var perfcount);
                return perfcount;
            }
        }

        /// <summary>
        /// Converts a number of ticks to performance timer counts.
        /// </summary>
        /// <param name="ticks">Number of ticks.</param>
        /// <returns>Number of performance timer counts corresponding to specified number of ticks, rounded up to nearest integer.</returns>
        public static long ConvertTicksToPerformanceCounts(long ticks)
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
        /// Converts a number of microseconds to performance timer counts.
        /// </summary>
        /// <param name="ticks">Number of ticks.</param>
        /// <returns>Number of performance timer counts corresponding to specified number of microseconds, rounded up to nearest integer.</returns>
        public static long ConvertMicrosecondsToPerformanceCounts(long microsec) =>
            ConvertTicksToPerformanceCounts(checked(microsec * ticks_per_microsecond));

        /// <summary>
        /// Converts a TimeSpan value to performance timer counts.
        /// </summary>
        /// <param name="ticks">Number of ticks.</param>
        /// <returns>Number of performance timer counts corresponding to TimeSpan value, rounded up to nearest integer number of performance timer counts.</returns>
        public static long ConvertToPerformanceCounts(this TimeSpan timespan) =>
            ConvertTicksToPerformanceCounts(timespan.Ticks);

        /// <summary>
        /// Converts a number of performance timer counts to ticks.
        /// </summary>
        /// <param name="perfcount">Number of performance timer counts.</param>
        /// <returns>Number of ticks corresponding to specified performance timer counts, rounded down to nearest integer.</returns>
        public static long ConvertPerformanceCountsToTicks(long perfcount) =>
            checked(perfcount * performance_counts_per_ticks_divisor / performance_counts_per_ticks_multiplier);

        /// <summary>
        /// Converts a number of performance timer counts to a TimeSpan value.
        /// </summary>
        /// <param name="perfcount">Number of performance timer counts.</param>
        /// <returns>TimeSpan value corresponding to specified performance timer counts, rounded down to nearest number of ticks that the TimeSpan structure can hold.</returns>
        public static TimeSpan ConvertPerformanceCountsToTimeSpan(long perfcount) =>
            TimeSpan.FromTicks(ConvertPerformanceCountsToTicks(perfcount));

        /// <summary>
        /// Waits specified number of performance timer counts by spinning in a tight loop until at least timer counts have passed. This can usually be used for very exact timing,
        /// since there is no asynchronous object models, Task objects or context switches used in the wait loop.
        /// 
        /// Number of performance timer counts per second can be found in <see cref="PerformanceCountsPerSecond">PerformanceCountsPerSecond</see> property.
        /// 
        /// You can convert microseconds, ticks or TimeSpan values to performance counts using support methods in this class.</see>
        /// </summary>
        /// <param name="perfcount">Number of performance timer counts to wait.</param>
        public static void SpinWaitPerformanceCounts(long perfcount)
        {
            perfcount += PerformanceCounterValue;

            while (PerformanceCounterValue < perfcount)
            {
            }
        }

        #region Internal implementation
        [DllImport("kernel32.dll")]
        extern private static long QueryPerformanceFrequency(out long frequency);

        [DllImport("kernel32.dll")]
        extern private static long QueryPerformanceCounter(out long count);

        private static readonly long ticks_per_microsecond = TimeSpan.FromSeconds(1).Ticks / 1000000;
        private static readonly long performance_counts_per_ticks_multiplier;
        private static readonly long performance_counts_per_ticks_divisor;

        static PerformanceTimers()
        {
            QueryPerformanceFrequency(out var freq);

            PerformanceCountsPerSecond = freq;

            var factor = NumericExtensions.GreatestCommonDivisor(PerformanceCountsPerSecond, TimeSpan.FromSeconds(1).Ticks);

            performance_counts_per_ticks_multiplier = PerformanceCountsPerSecond / factor;
            performance_counts_per_ticks_divisor = TimeSpan.FromSeconds(1).Ticks / factor;
        }
        #endregion
    }
}
