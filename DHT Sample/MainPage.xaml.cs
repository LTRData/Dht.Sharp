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
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Dht.Sample
{
    using Common;
    using Sharp;

    public partial class MainPage : BindablePage
	{
        private const int DataPinNumber = 24;

        private DispatcherTimer _timer = new DispatcherTimer();

		// ***
		// *** Define a reference for the sensor instance.
		// ***
		IDht _sensor = null;

		public MainPage()
		{
			InitializeComponent();

			// ***
			// *** Initialize the timer
			// ***
			_timer.Interval = TimeSpan.FromSeconds(2);
			_timer.Tick += Timer_Tick;
		}

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // ***
            // *** Get a reference to the GPIO Controller.
            // ***
            var controller = await GpioController.GetDefaultAsync();

            // ***
            // *** Make sure the reference is valid (that e are connected to a device with
            // *** a GPIO Controller.
            // ***
            if (controller == null)
            {
                return;
            }

            // ***
            // *** Set up the data pin.
            // ***
            var dataPin = controller.OpenPin(DataPinNumber, GpioSharingMode.Exclusive);

            // ***
            // *** Create the sensor.
            // ***
            _sensor = new Dht11(dataPin);

            // ***
            // *** Start the timer.
            // ***
            _timer.Start();
        }

		private async void Timer_Tick(object sender, object e)
		{
			try
			{
				// ***
				// *** Stop the timer so we do not have more than reading
				// *** at a time.
				// ***
				_timer.Stop();

				// ***
				// *** Increment the counter.
				// ***
				_totalRequests++;

				// ***
				// *** Read the sensor.
				// ***
				var reading = await _sensor.GetReadingAsync();

				// ***
				// *** Check the result.
				// ***
				if (reading.Result == DhtReadingResult.Valid)
                {
                    Trace.WriteLine($"Temperature {reading.Temperature:0.0} °C - Humidity {reading.Humidity:0.0} %");

                    Temperature = reading.Temperature;
					Humidity = reading.Humidity;

                    _successfulRequests++;
				}
                else
                {
                    Trace.WriteLine($"Sensor read failed: {reading.Result}");
                }
            }
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			finally
			{
				// ***
				// *** Start the timer again.
				// ***
				_timer.Start();
			}

			// ***
			// *** Update the success rate and running time.
			// ***
			RaisePropertyChanged(nameof(SuccessRate));
			RaisePropertyChanged(nameof(RunningTime));
		}

		private double _temperature = 0d;
		public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        private double _humidity = 0d;
		public double Humidity
        {
            get => _humidity;
            set => SetProperty(ref _humidity, value);
        }

        private int _totalRequests = 0;
		private int _successfulRequests = 0;
		public double SuccessRate
		{
			get
			{
				double returnValue = 0d;

				if (_totalRequests != 0)
				{
					returnValue = 100d * (double)_successfulRequests / (double)_totalRequests;
				}

				return returnValue;
			}
		}

		private DateTime _startedAt = DateTime.Now;

		public string RunningTime
		{
			get
			{
				string returnValue;

				TimeSpan ellapsed = DateTime.Now.Subtract(_startedAt);

				if (ellapsed.Days > 0)
				{
					returnValue = $"{ellapsed.Days} day(s), {ellapsed.Hours} hour(s), {ellapsed.Minutes} minutes(s) and {ellapsed.Seconds} second(s)";
				}
				else if (ellapsed.Hours > 0)
				{
					returnValue = $"{ellapsed.Hours} hour(s), {ellapsed.Minutes} minutes(s) and {ellapsed.Seconds} second(s)";
				}
				else if (ellapsed.Minutes > 0)
				{
					returnValue = $"{ellapsed.Minutes} minutes(s) and {ellapsed.Seconds} second(s)";
				}
				else
				{
					returnValue = $"{ellapsed.Seconds} second(s)";
				}

				return returnValue;
			}
		}
	}
}
