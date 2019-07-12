using System;
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
        /// <param name="dataPin">Specifies the GPIO pin used to read data from the sensor. This pin is connected
        /// directly to the data pin on the sensor.</param>
        public DhtBase(GpioPin dataPin)
        {
            DataPin = dataPin ?? throw new ArgumentNullException(nameof(dataPin));
        }

        /// <summary>
        /// Gets/sets the GPIO pin used to read data from the sensor. This pin is connected
        /// directly to the data pin on the sensor.
        /// </summary>
        public GpioPin DataPin { get; set; }

        /// <summary>
        /// Gets/sets a value in ms that indicates how long to wait for the sensor to 
        /// respond to a request for a reading. The default timeout is 100 ms.
        /// </summary>
        public int Timeout { get; set; } = 100;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        public async Task InitializeAsync()
        {
            // ***
            // *** Set the data pin HIGH.
            // ***
            DataPin.Write(GpioPinValue.High);
            DataPin.SetDriveMode(GpioPinDriveMode.Output);

            // ***
            // *** The data sheet states that the sensor should be given 1 second to initialize.
            // ***
            await Task.Delay(TimeSpan.FromSeconds(1));

            initialized = true;
        }

        private bool initialized;

        private static readonly long OneThreshold = (long)(110 * PerformanceTicksPerMicrosec);

        /// <summary>
        /// Gets a reading from the sensor.
        /// </summary>
        /// <returns>Returns an IDhtReading instance containing 
        /// the data from the sensor.</returns>
        public async Task<IDhtReading> GetReadingAsync()
        {
            if (!initialized)
            {
                await InitializeAsync();
            }

            // ***
            // *** Bring the line low for 18 ms (this is needed for the DHT11), the DHT22 does need
            // *** need as long.
            // ***
            DataPin.Write(GpioPinValue.High);
            DataPin.SetDriveMode(GpioPinDriveMode.Output);

            var init_delay = Task.Delay(20);

            var bits = new bool[40];

            await init_delay;

            DataPin.Write(GpioPinValue.Low);
            SpinWaitMicroseconds(18000);
            DataPin.Write(GpioPinValue.High);
            SpinWaitMicroseconds(40);
            DataPin.SetDriveMode(GpioPinDriveMode.Input);
            SpinWaitMicroseconds(10);

            // ***
            // *** Capture every falling edge until all bits are received or
            // *** timeout occurs
            // ***
            var endTickCount = GetTickCount64() + Timeout;

            var previousValue = DataPin.Read();

            var prevTime = 0L;

            for (var i = -1; i < bits.Length;)
            {
                if (GetTickCount64() > endTickCount)
                {
                    return DhtReading.FromTimeout();
                }

                var value = DataPin.Read();

                if ((previousValue == GpioPinValue.High) && (value == GpioPinValue.Low))
                {
                    // ***
                    // *** A falling edge was detected
                    // ***
                    var now = PerformanceCounter;

                    if (i >= 0)
                    {
                        var difference = unchecked(now - prevTime);
                        bits[i] = difference > OneThreshold;
                    }

                    prevTime = now;
                    ++i;
                }

                previousValue = value;
            }

            // ***
            // *** Convert the bit records to the 5 bytes of data that the sensor
            // *** sends out.
            // ***
            var data = bits.ToByteArray();

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
    }
}
