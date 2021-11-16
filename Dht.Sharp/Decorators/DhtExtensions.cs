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
        public static byte ExpectedChecksum(this byte[] data) =>
            data[4];

        /// <summary>
        /// Calculates the checksum of the first 4 bytes of data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte ActualChecksum(this byte[] data) =>
            (byte)(data[0] + data[1] + data[2] + data[3]);

        /// <summary>
        /// Compares the expected and actual checksums.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool HasValidChecksum(this byte[] data) =>
            data.ExpectedChecksum() == data.ActualChecksum() &&
            data.Any(b => !b.Equals(0));

        /// <summary>
        /// Sets a bit to 1 in a bit field.
        /// </summary>
        /// <param name="data">Bit field</param>
        /// <param name="bitnumber">Bit number to set to 1</param>
        public static void SetBit(this byte[] data, int bitnumber) =>
            data[bitnumber >> 3] |= (byte)(1 << ((~bitnumber) & 7));

        /// <summary>
        /// Sets a bit to 0 in a bit field.
        /// </summary>
        /// <param name="data">Bit field</param>
        /// <param name="bitnumber">Bit number to set to 0</param>
        public static void ClearBit(this byte[] data, int bitnumber) =>
            data[bitnumber >> 3] &= unchecked((byte)~(1 << ((~bitnumber) & 7)));

        /// <summary>
        /// Gets a bit from a bit field.
        /// </summary>
        /// <param name="data">Bit field</param>
        /// <param name="bitnumber">Bit number to get/param>
        /// <returns>True if value of specified bit is 1, false if 0.</returns>
        public static bool GetBit(this byte[] data, int bitnumber) =>
            (data[bitnumber >> 3] & (1 << ((~bitnumber) & 7))) != 0;
    }
}
