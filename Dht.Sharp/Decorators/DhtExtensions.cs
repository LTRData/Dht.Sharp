// Copyright © 2018 Daniel Porrey
//
// This file is part of the Dht11 Solution.
// 
// Dht.Sharp Solution is free software: you can redistribute it and/or modify
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
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Gpio;

namespace Dht.Sharp
{
	internal static class DhtExtensions
	{
		/// <summary>
		/// Converts the change record intervals into bits and then into a
		/// 5 byte array.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static byte[] ToByteArray(this IList<GpioChangeRecord> data)
		{
			byte[] returnValue = new byte[15];

			// ***
			// *** The change records contain the time of each input change from 5v (high)
			// *** to 0v (low). This is the falling edge. The time between each falling edge
			// *** is calculated to determine if the data being sent is a 1 or a 0. The first
			// *** two edges are the trigger and the response and must be ignored. The remaining
			// *** 41 represents 40 intervals which is 40 bits or 5 bytes.
			// ***
			for (int i = 3; i < data.Count; i++)
			{
				// ***
				// *** Calculate the total time interval between falling edges.
				// ***
				int interval = (int)((data[i].RelativeTime - data[i - 1].RelativeTime).TotalMilliseconds * 1000);

				// ***
				// *** Get the value of the next bit.
				// ***
				byte bit = interval > 110 ? (byte)1 : (byte)0;

				// ***
				// *** Get the index of the byte we are filling. The sensor sends
				// *** a total of 5 bytes.
				// ***
				int index = (int)Math.Floor((i - 3) / 8D);

				// ***
				// *** Shift the bits one to the left.
				// ***
				returnValue[index] <<= 1;

				// ***
				// *** Add the next bit to the byte.
				// ***
				returnValue[index] += bit;
			}

			return returnValue;
		}

        /// <summary>
        /// Converts the change record intervals into bits and then into a
        /// 5 byte array.
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this IList<bool> bits)
        {
            var data = new byte[bits.Count >> 3];

            // ***
            // *** The change records contain the time of each input change from 5v (high)
            // *** to 0v (low). This is the falling edge. The time between each falling edge
            // *** is calculated to determine if the data being sent is a 1 or a 0. The first
            // *** two edges are the trigger and the response and must be ignored. The remaining
            // *** 41 represents 40 intervals which is 40 bits or 5 bytes.
            // ***
            for (var i = 0; i < bits.Count; i++)
            {
                // ***
                // *** Get the index of the byte we are filling. The sensor sends
                // *** a total of 5 bytes.
                // ***
                int index = i >> 3;

                // ***
                // *** Add the next bit to the byte.
                // ***
                if (bits[i])
                {
                    data[index] += (byte)(1 << ((~i) & 7));
                }
            }

            return data;
        }

        /// <summary>
        /// Returns the checksum value given by the sensor.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte ExpectedChecksum(this byte[] data)
		{
			return data[4];
		}

		/// <summary>
		/// Calculates the checksum of the first 4 bytes of data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static byte ActualChecksum(this byte[] data)
		{
			return (byte)(data[0] + data[1] + data[2] + data[3]);
		}

		/// <summary>
		/// Compares the expected and actual checksums.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool HasValidChecksum(this byte[] data)
		{
			return data.ExpectedChecksum() == data.ActualChecksum();
		}
	}
}
