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
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using static LTRLib.LTRGeneric.PerformanceCounter;

namespace Dht.Sharp
{
    /// <summary>
    /// Base class for IDht sensors.
    /// </summary>
    public abstract class DhtBase : IDht
    {
        /// <summary>
        /// Creates an instance of Dht.Sharp.DhtBase with the given Data Pin.
        /// </summary>
        /// <param name="gpio_pin">Specifies the GPIO pin used to read data from the sensor. This pin is connected
        /// directly to the data pin on the sensor.</param>
        public DhtBase(GpioPin gpio_pin)
        {
            pin = gpio_pin ?? throw new ArgumentNullException(nameof(gpio_pin));
        }

        /// <summary>
        /// Gets/sets the GPIO pin used to read data from the sensor. This pin is connected
        /// directly to the data pin on the sensor.
        /// </summary>
        private GpioPin pin;

        public int PinNumber => pin.PinNumber;

        /// <summary>
        /// Gets/sets a value in ms that indicates how long to wait for the sensor to 
        /// respond to a request for a reading. The default timeout is 40 ms.
        /// 
        /// This timeout uses a low-accuracy timer which typically does not work well with
        /// small timeout values, which could lead to unexpected timeouts if a lower value
        /// than about 40 ms is used.
        /// </summary>
        public int ReadTimeout { get; set; } = 40;

        /// <summary>
        /// Gets/sets number of times to retry on timeouts, checksum errors etc.
        /// </summary>
        public int RetryCount { get; set; } = 5;

        /// <summary>
        /// Delay in ms when initializing sensor before first reading or after failed readings.
        /// </summary>
        public int InitializationDelay { get; set; } = 1000;

        /// <summary>
        /// Delay in ms when reinitializing sensor for a new reading after a successful reading.
        /// </summary>
        public int ReinitializationDelay { get; set; } = 20;

        /// <summary>
        /// Minimum interval in ms required by sensor between readings. Default is 1000 ms
        /// for DHT11 and 2000 ms for DHT22.
        /// </summary>
        public int MinSampleInterval { get; set; }

        private long last_success_timestamp;

        private static readonly long OneThreshold = ConvertMicrosecondsToPerformanceCounts(110);

        /// <summary>
        /// Gets a reading from the sensor.
        /// </summary>
        /// <returns>Returns an IDhtReading instance containing 
        /// the data from the sensor.</returns>
        public async Task<IDhtReading> GetReadingAsync()
        {
            IDhtReading reading = null;

            for (var attempt = 0; attempt <= RetryCount; attempt++)
            {
                pin.Write(GpioPinValue.High);
                pin.SetDriveMode(GpioPinDriveMode.Output);

                if (last_success_timestamp == 0)
                {
                    await Task.Delay(InitializationDelay);
                }
                else if (GetTickCount64() - last_success_timestamp < MinSampleInterval)
                {
                    var delay = Math.Max(MinSampleInterval - (int)(GetTickCount64() - last_success_timestamp), ReinitializationDelay);

                    await Task.Delay(delay);
                }
                else
                {
                    await Task.Delay(ReinitializationDelay);
                }

                reading = GetReading();

                if (reading.Result == DhtReadingResult.Valid)
                {
                    last_success_timestamp = GetTickCount64();
                    break;
                }

                last_success_timestamp = 0;

#if DEBUG
                Debug.WriteLine($"Sensor read failed: {reading.Result}, attempt {attempt}");
#endif
            }

            return reading;
        }

        private static readonly long perf_counts_18ms = ConvertMicrosecondsToPerformanceCounts(18000);
        private static readonly long perf_counts_40us = ConvertMicrosecondsToPerformanceCounts(40);
        private static readonly long perf_counts_10us = ConvertMicrosecondsToPerformanceCounts(10);

        private const int DataBufferBytes = 5;

        private const int DataBufferBits = DataBufferBytes * 8;

        private readonly byte[] DataBuffer = new byte[DataBufferBytes];

        private IDhtReading GetReading()
        {
            Array.Clear(DataBuffer, 0, DataBuffer.Length);

            // ***
            // *** Bring the line low for 18 ms (this is needed for the DHT11), the DHT22 does need
            // *** need as long.
            // ***
            pin.Write(GpioPinValue.Low);
            SpinWaitPerformanceCounts(perf_counts_18ms);
            pin.Write(GpioPinValue.High);
            SpinWaitPerformanceCounts(perf_counts_40us);
            pin.SetDriveMode(GpioPinDriveMode.Input);
            SpinWaitPerformanceCounts(perf_counts_10us);

            // ***
            // *** Capture every falling edge until all bits are received or
            // *** timeout occurs
            // ***
            var endTickCount = GetTickCount64() + ReadTimeout;

            var previousValue = pin.Read();

            var prevTime = 0L;

            for (var i = -1; i < DataBufferBits;)
            {
                if (GetTickCount64() > endTickCount)
                {
                    return DhtReading.FromTimeout();
                }

                var value = pin.Read();

                if ((previousValue == GpioPinValue.High) && (value == GpioPinValue.Low))
                {
                    // ***
                    // *** A falling edge was detected
                    // ***
                    var now = PerformanceCounterValue;

                    if (i >= 0)
                    {
                        var difference = unchecked(now - prevTime);
                        if (difference > OneThreshold)
                        {
                            DataBuffer.SetBit(i);
                        }
                    }

                    prevTime = now;
                    ++i;
                }

                previousValue = value;
            }

            // ***
            // *** Convert the 5 bytes of data to an IDhtReading instance.
            // ***
            return ParseData(DataBuffer);
        }

        private IDhtReading ParseData(byte[] data)
        {
            // ***
            // *** Verify the checksum.
            // ***
            if (data.HasValidChecksum())
            {
                // ***
                // *** This is a valid reading, convert the temperature and humidity.
                // ***
                return new DhtReading
                {
                    Temperature = ParseTemperature(data),
                    Humidity = ParseHumidty(data),
                    Result = DhtReadingResult.Valid
                };
            }
            else
            {
                // ***
                // *** The checksum did not match.
                // ***
                return new DhtReading
                {
                    Temperature = 0d,
                    Humidity = 0d,
                    Result = DhtReadingResult.ChecksumError
                };
            }
        }

        /// <summary>
        /// Converts the byte data to a temperature value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract double ParseTemperature(byte[] data);

        /// <summary>
        /// Converts the byte data to a humidity value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract double ParseHumidty(byte[] data);

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    pin?.Dispose();
                }

                // TODO: set large fields to null.
                pin = null;

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
    }
}
