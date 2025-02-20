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

namespace Dht.Sharp;

/// <summary>
/// Defines an interface for a reading from a DHT sensor.
/// </summary>
public readonly record struct DhtReading
{
    /// <summary>
    /// Gets the result of the sensor reading.
    /// </summary>
    public DhtReadingResult Result { get; init; }

	/// <summary>
	/// Gets the temperature from sensor.
	/// </summary>
	public double Temperature { get; init; }

	/// <summary>
	/// Gets the humidity from the sensor.
	/// </summary>
	public double Humidity { get; init; }

    internal static DhtReading FromTimeout() => new()
    {
        Temperature = 0d,
        Humidity = 0d,
        Result = DhtReadingResult.Timeout
    };
}

/// <summary>
/// Represents the result of a DHT sensor reading.
/// </summary>
public enum DhtReadingResult
{
    /// <summary>
    /// Indicates that no reading has been taken.
    /// </summary>
    None,
    /// <summary>
    /// Indicates the reading is valid.
    /// </summary>
    Valid,
    /// <summary>
    /// Indicates that the sensor did not respond to the request for a reading.
    /// </summary>
    Timeout,
    /// <summary>
    /// Indicates that the reading from the sensor was invalid.
    /// </summary>
    ChecksumError
}
