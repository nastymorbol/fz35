using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FZ35;

namespace Demo
{
    class Program
    {
        private static Connection _connection;
        private static DateTime _lastRecord;
        private static List<CapacityRecord> _records = new List<CapacityRecord>();
        private static ConsoleChart _consolechart;
        private static double _currentLoad = .4;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Read FZ35!");
            Console.CursorVisible = false;
            _consolechart = new ConsoleChart( height: Console.WindowHeight-2);
            
            try
            {
                _connection = await new FZ35.Connection("192.168.123.63", 8888)
                    .SetLoadCurrent(_currentLoad)
                    .SetOverCurrentProtection()
                    .SetMaximumCapacity()
                    .SetLowVoltageProtection(1.5)
                    .SetOverPowerProtection()
                    .SetOverVoltageProtection()
                    .SetMaximumDischargeTime(TimeSpan.FromHours(0))
                    .ExecuteCommandsAsync();

                await _connection.TurnOnLoad();
                await _connection.StartPeriodicMeasurement();

                _connection.OnRecordReceived += OnRecordReceived;
                _connection.OnErrorReceived += OnErrorReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            _lastRecord = DateTime.Now;

            while (true)
            {
                //_consolechart.Add(rnd.NextDouble() * 100);
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void OnErrorReceived(object sender, string e)
        {
            _connection.TurnOffLoad();
        }

        private static async void OnRecordReceived(object sender, CapacityRecord e)
        {
            _consolechart.Add(e.Voltage);
            
            Console.WriteLine($"Energy: {e.Energy:F3} mAh | Current: {e.Ampere:F2} A | Voltage: {e.Voltage:F2} | Power: {e.Power:F} W".PadRight(Console.WindowWidth));
            if ((DateTime.Now - _lastRecord).TotalSeconds > 10)
            {
                _lastRecord = DateTime.Now;
                _records.Add(e);

                var json = System.Text.Json.JsonSerializer.Serialize(_records);
                System.IO.File.WriteAllText("../../../data.json", json);
            }

            /*
            if (e.Voltage > 4.5)
                _currentLoad = e.Ampere + .2;

            else if (e.Voltage > 4.2)
                _currentLoad = e.Ampere + .05;
            
            else if (e.Voltage < 4.0 && e.Ampere > .5)
                _currentLoad = e.Ampere - .1;
            */
            
            if(e.Power < 4.8)
                _currentLoad = e.Ampere + .01;
            else if(e.Power > 5.0)
                _currentLoad = e.Ampere - .05;
            
            if (e.Voltage < 3.0 && e.Ampere > .5)
                _currentLoad = e.Ampere - .01;
            
            if(_currentLoad != e.Ampere)
                await _connection.SetLoadCurrent(_currentLoad)
                    .ExecuteCommandsAsync();
        }
    }
}
