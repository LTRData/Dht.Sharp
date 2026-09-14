# Dht.Sharp

C# library for reading DHT11 and DHT22 temperature and humidity sensors through a single GPIO data pin per sensor.

This is Olof Lagerkvist's LTR Data modification of [Daniel Porrey's Dht.Sharp](https://github.com/porrey/Dht.Sharp), originally developed for Windows IoT Core on Raspberry Pi. The LTRData implementation measures GPIO pulse timing using a high-resolution performance counter (`Stopwatch`) instead of the original GPIO ChangeReader approach. The current library uses `System.Device.Gpio` and targets **.NET 8, .NET 9 and .NET 10**.

## Package and hardware

Install [LTRData.Dht.Sharp](https://www.nuget.org/packages/LTRData.Dht.Sharp) in a compatible .NET application:

```sh
dotnet add package LTRData.Dht.Sharp
```

The namespace remains `Dht.Sharp`. `System.Device.Gpio` is a package dependency.

Windows and Linux operation requires GPIO hardware and a suitable `System.Device.Gpio` driver, with permission to access the pins. Framework compatibility alone does not provide GPIO access on an ordinary PC. See the [.NET IoT repository](https://github.com/dotnet/iot) for GPIO platform and driver documentation.

Connect the sensor's data line to one GPIO pin, with power, ground and any required data-line pull-up wired according to the sensor/module and host board specifications. Use GPIO-compatible voltage levels.

| Sensor class | Measurement | Default minimum sampling interval |
|---|---|---|
| `Dht11` | Temperature in °C and relative humidity in % | 1 second |
| `Dht22` | Temperature in °C, including negative values, and relative humidity in % | 2 seconds |

## Read a sensor

The constructor accepts an open `System.Device.Gpio.GpioPin`. For example, in a console application's `Program.cs`:

```csharp
using System;
using System.Device.Gpio;
using Dht.Sharp;

using var controller = new GpioController();

// Choose the pin number for your wiring and GPIO driver.
using var dataPin = controller.OpenPin(26, PinMode.Output, PinValue.High);
var sensor = new Dht22(dataPin); // Use Dht11 for a DHT11 sensor.

var reading = await sensor.GetReadingAsync();

if (reading.Result == DhtReadingResult.Valid)
{
    Console.WriteLine(
        $"Temperature: {reading.Temperature:0.0} °C; humidity: {reading.Humidity:0.0} %");
}
else
{
    Console.WriteLine($"Sensor read failed: {reading.Result}");
}
```

The application owns the pin and controller; keep both alive while reading and dispose them afterwards. The sensor switches the pin between output and input as part of the protocol.

Always check `Result` before using the measurements. Failed reads report `Timeout` or `ChecksumError`; their zero-valued temperature and humidity are not valid measurements. GPIO access errors can also throw exceptions.

Await each call before starting another read on the same sensor. `GetReadingAsync()` applies the minimum interval after successful reads and retries failed attempts. Defaults are five retries after the initial attempt, a 40 ms read timeout, a 1,000 ms initialization delay and a 20 ms reinitialization delay. These are configurable through `RetryCount`, `ReadTimeout`, `InitializationDelay`, `ReinitializationDelay` and `MinSampleInterval`.

Although the API is asynchronous, pulse capture uses synchronous polling and busy waits. Timing depends on the GPIO driver and system scheduling; applications should handle failed readings.

## Build and historical sample

With the .NET 10 SDK installed, build the library directly from the repository root:

```sh
dotnet build Dht.Sharp/Dht.Sharp.csproj -c Debug -f net10.0
```

Debug builds avoid the project's automatic Release package generation. Release builds are configured to produce the NuGet package; shared properties use `LocalNuGetPath` as the package output path.

The [DHT Sample](https://github.com/LTRData/Dht.Sharp/tree/master/DHT%20Sample) directory contains the historical Windows IoT Core/UWP application. It still uses `Windows.Devices.Gpio` and the old `IDht` interface, and its UWP target cannot reference the current .NET 8–10 library. It is retained as historical source and needs migration before use with this version. The solution includes that sample, so use the library project command above for a current build.

## License and attribution

The source headers specify the GNU General Public License, version 3 or later. They credit Daniel Porrey and Olof Lagerkvist; the performance-timer helpers also carry Olof Lagerkvist's copyright. See the source headers and the [GPLv3 text](https://www.gnu.org/licenses/gpl-3.0.html).
