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
using Windows.Devices.Gpio;

namespace Dht.Sharp
{
    /// <summary>
    /// An instance of IDht used specifically for the DHT11 sensor.
    /// </summary>
    public class Dht11 : DhtBase
    {
        /// <summary>
        /// Creates an instance of Dht.Sharp.Dht22 with the given Data Pin
        /// and Trigger Pin.
        /// </summary>
        /// <param name="dataPin">Specifies the GPIO pin used to read data from the sensor. This pin is connected
        /// directly to the data pin on the sensor.</param>
        public Dht11(GpioPin dataPin)
            : base(dataPin)
        {
        }

        /// <summary>
        /// Converts the byte data to a temperature value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override double ParseTemperature(byte[] data)
        {
            // ***
            // *** Get the temperature from bytes 2 and 3.
            // ***
            var returnValue = data[2] + data[3] / 10d;

            return returnValue;
        }

        /// <summary>
        /// Converts the byte data to a humidity value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override double ParseHumidty(byte[] data)
        {
            // ***
            // *** Get the humidity from bytes 0 and 1
            // ***
            var returnValue = data[0] + data[1] / 10d;

            return returnValue;
        }
    }
}
