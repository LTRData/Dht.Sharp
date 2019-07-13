using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Dht.Sharp
{
    using static NativeTimerFunctions;

    /// <summary>
    /// Base class for IDht sensors.
    /// </summary>
    public abstract class DhtBase : IDht
    {
        /// <summary>
        /// Creates an instance of Dht.Sharp.Dht22 with the given Data Pin
        /// and Trigger Pin.
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

        /// <summary>
        /// Gets/sets a value in ms that indicates how long to wait for the sensor to 
        /// respond to a request for a reading. The default timeout is 100 ms.
        /// </summary>
        public int ReadTimeout { get; set; } = 100;

        /// <summary>
        /// Gets/sets number of times to retry on timeouts, checksum errors etc.
        /// </summary>
        public int RetryCount { get; set; } = 5;

        /// <summary>
        /// Delay when initializing sensor before first reading or after failed readings.
        /// </summary>
        public TimeSpan InitializationDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Delay when reinitializing sensor for a new reading after a successful reading.
        /// </summary>
        public TimeSpan ReinitializationDelay { get; set; } = TimeSpan.FromMilliseconds(20);

        private bool initialized;

        private static readonly long OneThreshold = (long)(110 * PerformanceTicksPerMicrosec);

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

                if (initialized)
                {
                    await Task.Delay(ReinitializationDelay);
                }
                else
                {
                    await Task.Delay(InitializationDelay);
                }

                reading = GetReading();

                if (reading.Result == DhtReadingResult.Valid)
                {
                    initialized = true;
                    break;
                }

                initialized = false;

#if DEBUG
                Trace.WriteLine($"Sensor read failed: {reading.Result}, attempt {attempt}");
#endif
            }

            return reading;
        }

        private IDhtReading GetReading()
        {
            var data = new byte[5];

            // ***
            // *** Bring the line low for 18 ms (this is needed for the DHT11), the DHT22 does need
            // *** need as long.
            // ***
            pin.Write(GpioPinValue.Low);
            SpinWaitMicroseconds(18000);
            pin.Write(GpioPinValue.High);
            SpinWaitMicroseconds(40);
            pin.SetDriveMode(GpioPinDriveMode.Input);
            SpinWaitMicroseconds(10);

            // ***
            // *** Capture every falling edge until all bits are received or
            // *** timeout occurs
            // ***
            var endTickCount = GetTickCount64() + ReadTimeout;

            var previousValue = pin.Read();

            var prevTime = 0L;

            for (var i = -1; i < 40;)
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
                    var now = PerformanceCounter;

                    if (i >= 0)
                    {
                        var difference = unchecked(now - prevTime);
                        if (difference > OneThreshold)
                        {
                            data[i >> 3] |= (byte)(1 << ((~i) & 7));
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
            return ParseData(data);
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
