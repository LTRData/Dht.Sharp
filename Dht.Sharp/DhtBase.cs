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

using Dht.Sharp.Decorators;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Device.Gpio;
using static Dht.Sharp.LTRLib.PerformanceTimers;

namespace Dht.Sharp;

/// <summary>
/// Base class for IDht sensors.
/// </summary>
/// <remarks>
/// Creates an instance of Dht.Sharp.DhtBase with the given Data Pin.
/// </remarks>
/// <param name="gpioPin">Specifies the GPIO pin used to read data from the sensor. This pin is connected
/// directly to the data pin on the sensor.</param>
public abstract class DhtBase(GpioPin gpioPin)
{
    /// <summary>
    /// Gets/sets the GPIO pin used to read data from the sensor. This pin is connected
    /// directly to the data pin on the sensor.
    /// </summary>
    private readonly GpioPin pin = gpioPin ?? throw new ArgumentNullException(nameof(gpioPin));

    /// <inheritdoc/>
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

    private static readonly long OneThreshold = ConvertMicrosecondsToPerformanceCounts(100);

    /// <summary>
    /// Gets a reading from the sensor.
    /// </summary>
    /// <returns>Returns an IDhtReading instance containing 
    /// the data from the sensor.</returns>
    public async Task<DhtReading> GetReadingAsync()
    {
        DhtReading reading = default;

        for (var attempt = 0; attempt <= RetryCount; attempt++)
        {
            pin.Write(PinValue.High);
            pin.SetPinMode(PinMode.Output);

            if (last_success_timestamp == 0)
            {
                await Task.Delay(InitializationDelay).ConfigureAwait(false);
            }
            else if (Environment.TickCount64 - last_success_timestamp < MinSampleInterval)
            {
                var delay = Math.Max(MinSampleInterval - (int)(Environment.TickCount64 - last_success_timestamp), ReinitializationDelay);

                await Task.Delay(delay).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(ReinitializationDelay).ConfigureAwait(false);
            }

            reading = GetReading();

            if (reading.Result == DhtReadingResult.Valid)
            {
                last_success_timestamp = Environment.TickCount64;
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

    private DhtReading GetReading()
    {
        Span<byte> data = stackalloc byte[DataBufferBytes];

        data.Clear();

        // ***
        // *** Bring the line low for 18 ms (this is needed for the DHT11), the DHT22 does need
        // *** need as long.
        // ***
        pin.Write(PinValue.Low);
        SpinWaitPerformanceCounts(perf_counts_18ms);
        pin.Write(PinValue.High);
        SpinWaitPerformanceCounts(perf_counts_40us);
        pin.SetPinMode(PinMode.Input);
        SpinWaitPerformanceCounts(perf_counts_10us);

        // ***
        // *** Capture every falling edge until all bits are received or
        // *** timeout occurs
        // ***
        var endTickCount = Environment.TickCount64 + ReadTimeout;

        var previousValue = pin.Read();

        var prevTime = 0L;

        for (var i = -1; i < DataBufferBits;)
        {
            if (Environment.TickCount64 > endTickCount)
            {
                return DhtReading.FromTimeout();
            }

            var value = pin.Read();

            if (previousValue == PinValue.High && value == PinValue.Low)
            {
                // ***
                // *** A falling edge was detected
                // ***
                var now = GetPerformanceCounterValue();

                if (i >= 0)
                {
                    var difference = unchecked(now - prevTime);

                    if (difference > OneThreshold)
                    {
                        data[i >> 3] |= (byte)(1 << (7 - (i & 7)));
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

    private DhtReading ParseData(ReadOnlySpan<byte> data)
    {
        // ***
        // *** Verify the checksum.
        // ***
        if (DhtExtensions.IsValidReading(data))
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
    protected abstract double ParseTemperature(ReadOnlySpan<byte> data);

    /// <summary>
    /// Converts the byte data to a humidity value.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected abstract double ParseHumidty(ReadOnlySpan<byte> data);
}
