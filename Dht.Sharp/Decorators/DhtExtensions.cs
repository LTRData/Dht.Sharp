// Copyright © 2018 Daniel Porrey, modified by Olof Lagerkvist 2019
//
// This file is part of the DhtSharp Solution.
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
