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

using System.Collections.Generic;
using System.Linq;

namespace LTRLib.Extensions
{
    /// <summary>
    /// Static methods for factorization of integer values.
    /// </summary>
    public static class NumericExtensions
    {
        /// <summary>
        /// Finds greatest common divisor for two values.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <returns>Greatest common divisor for values a and b.</returns>
        public static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Finds greatest common divisor for two values.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <returns>Greatest common divisor for values a and b.</returns>
        public static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Finds greatest common divisor for a sequence of values.
        /// </summary>
        /// <param name="values">Sequence of values</param>
        /// <returns>Greatest common divisor for values.</returns>
        public static int GreatestCommonDivisor(this IEnumerable<int> values) =>
            values.Aggregate(GreatestCommonDivisor);

        /// <summary>
        /// Finds greatest common divisor for a sequence of values.
        /// </summary>
        /// <param name="values">Sequence of values</param>
        /// <returns>Greatest common divisor for values.</returns>
        public static long GreatestCommonDivisor(this IEnumerable<long> values) =>
            values.Aggregate(GreatestCommonDivisor);

        /// <summary>
        /// Finds least common multiple for two values.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <returns>Least common multiple for values a and b.</returns>
        public static int LeastCommonMultiple(int a, int b) =>
            (a / GreatestCommonDivisor(a, b)) * b;

        /// <summary>
        /// Finds least common multiple for two values.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <returns>Least common multiple for values a and b.</returns>
        public static long LeastCommonMultiple(long a, long b) =>
            (a / GreatestCommonDivisor(a, b)) * b;

        /// <summary>
        /// Finds least common multiple for a sequence of values.
        /// </summary>
        /// <param name="values">Sequence of values</param>
        /// <returns>Least common multiple for values.</returns>
        public static int LeastCommonMultiple(this IEnumerable<int> values) =>
            values.Aggregate(LeastCommonMultiple);

        /// <summary>
        /// Finds least common multiple for a sequence of values.
        /// </summary>
        /// <param name="values">Sequence of values</param>
        /// <returns>Least common multiple for values.</returns>
        public static long LeastCommonMultiple(this IEnumerable<long> values) =>
            values.Aggregate(LeastCommonMultiple);

        /// <summary>
        /// Returns a sequence of prime factors for a value.
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Sequence of prime factors</returns>
        public static IEnumerable<long> PrimeFactors(this int value)
        {
            var z = 2;

            while (checked(z * z) <= value)
            {
                if (value % z == 0)
                {
                    yield return z;
                    value /= z;
                }
                else
                {
                    z++;
                }
            }

            if (value > 1)
            {
                yield return value;
            }
        }

        /// <summary>
        /// Returns a sequence of prime factors for a value.
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Sequence of prime factors</returns>
        public static IEnumerable<long> PrimeFactors(this long value)
        {
            var z = 2L;

            while (checked(z * z) <= value)
            {
                if (value % z == 0)
                {
                    yield return z;
                    value /= z;
                }
                else
                {
                    z++;
                }
            }

            if (value > 1)
            {
                yield return value;
            }
        }
    }
}

