using System.ComponentModel.Design;
using System.Collections.Generic;
using System.Text;
using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using FZ35.Commands;
using SimpleTCP;

namespace FZ35
{
    public class Connection
    {
        private readonly SimpleTcpClient _client;
        private readonly StringBuilder _buffer;
        private bool _periodicMeasurement;
        private DateTime _loadOnStartTime;
        private TimeSpan _loadInterval;

        private List<IFz35Command> _commands;
        private List<CapacityRecord> _records;

        public EventHandler<CapacityRecord> OnRecordReceived;
        
        public EventHandler<string> OnErrorReceived;

        public Connection(string ip, int port)
        {
            _client = new SimpleTCP.SimpleTcpClient();
            _buffer = new StringBuilder();

            //_client.TimeOut = TimeSpan.FromSeconds(1);
            //_client.DataReceived += Client_DataReceived;            
            _client.Connect(ip, port);
            _client.Delimiter = 10;
            _client.DelimiterDataReceived += Client_DataReceived;
            _commands = new List<IFz35Command>();
        }

        private void Client_DataReceived(object sender, Message e)
        {
            Debug.WriteLine($"{DateTime.Now} TCP Data: " + e.MessageString.Trim());
            if (_records == null)
                _records = new List<CapacityRecord>();

            if (e.MessageString.Trim().Length == 3)
            {
                OnErrorReceived?.Invoke(this, e.MessageString);
                _buffer.Append(e.MessageString.Trim());
            }
            // 03.84V,0.10A,0.001Ah,00:02
            var data = e.MessageString.Trim().Split(',');
            if (data.Length == 4)
            {
                if (IsSuccess(e.MessageString))
                    _buffer.Append(e.MessageString);
                else if(IsFail(e.MessageString))
                    _buffer.Append(e.MessageString);
                 
                _periodicMeasurement = true;
                var record = new CapacityRecord
                {
                    TimeStamp = DateTimeOffset.Now,
                    Voltage = double.Parse(data[0].Replace("V", ""), NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo),
                    Ampere = double.Parse(data[1].Replace("A", ""), NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo),
                    Energy = double.Parse(data[2].Replace("Ah", ""), NumberStyles.Any,
                        System.Globalization.NumberFormatInfo.InvariantInfo),
                    RemainingTime = TimeSpan.ParseExact(data[3], "hh\\:mm", null).TotalSeconds
                };

                _records.Add(record);
                
                OnRecordReceived?.Invoke(this, record);
                
                Debug.WriteLine($"{record}");
            }
            else
            {
                _buffer.Append(e.MessageString.Trim());
            }

        }

        public Connection SetLoadCurrent(double load)
        {
            var cmd = Commands.Fz35CommandFactory.LoadCurrent(load);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetLowVoltageProtection(double load)
        {
            var cmd = Commands.Fz35CommandFactory.SetLowVoltageProtection(load);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetOverVoltageProtection(double load = 25.2)
        {
            // OUP
            var cmd = Commands.Fz35CommandFactory.OverVoltageProtection(load);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetOverCurrentProtection(double load = 4.1)
        {
            // OCP
            var cmd = Commands.Fz35CommandFactory.SetOverCurrentProtection(load);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetOverPowerProtection(double load = 25.5)
        {
            // OPP
            var cmd = Commands.Fz35CommandFactory.SetOverPowerProtection(load);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetMaximumCapacity(double value = 0.0)
        {
            // OAH
            var cmd = Commands.Fz35CommandFactory.SetMaximumCapacity(value);
            _commands.Add(cmd);

            return this;
        }

        public Connection SetMaximumDischargeTime(TimeSpan value = default)
        {
            // OHP:xx:xx
            var cmd = Commands.Fz35CommandFactory.SetMaximumDischargeTime(value);
            _commands.Add(cmd);

            return this;
        }

        public async Task<bool> TurnOnLoad(TimeSpan interval = default)
        {
            // OHP:xx:xx
            var cmd = "on";
            var result = await SendAndWaitAsync(cmd);

            if (interval != default)
            {
                _loadOnStartTime = DateTime.Now;
            }
            else
            {
                _loadOnStartTime = default;
            }

            _loadInterval = interval;

            return IsSuccess(result);
        }

        public async Task<bool> TurnOffLoad()
        {
            // OHP:xx:xx
            var cmd = "off";
            var result = await SendAndWaitAsync(cmd);
            _loadOnStartTime = default;
            return IsSuccess(result);
        }

        public async Task<Connection> ExecuteCommandsAsync()
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                var cmd = _commands[i];
                var req = cmd.Get();
                var res =  await SendAndWaitAsync(req);
                if (IsSuccess(res))
                {
                    _commands.RemoveAt(i);
                    i--;
                    System.Threading.Thread.Sleep(20);
                }
                else
                {
                    _commands.RemoveAt(i);
                    throw new EvaluateException($"Command not executed {req}");
                }
            }
            
            return this;
        }

        private bool IsSuccess(string result)
        {
            return result.Contains("suc");
        }
        private bool IsFail(string result)
        {
            return result.Contains("fail");
        }

        public Task StartPeriodicMeasurement()
        {
            return PeriodicMeasurement(true);
        }

        public Task StopPeriodicMeasurement()
        {
            return PeriodicMeasurement(false);
        }

        private Task<string> SendAndWaitAsync(string cmd)
        {
            return Task.Run(() =>
            {
                _buffer.Clear();
                Debug.WriteLine($"{DateTime.Now} Send: {cmd}");
                _client.Write(cmd);

                var count = 60;
                while (_buffer.Length == 0)
                {
                    if (count-- < 1)
                        break;

                    if (count % 10 == 0)
                    {
                        Debug.WriteLine($"{DateTime.Now} Retry Send: {cmd}");

                        _client.Write(cmd);
                    }

                    System.Threading.Thread.Sleep(100);
                }

                while (count > 0)
                {
                    var s = _buffer.ToString();
                    if (IsSuccess(s) || IsSuccess(s))
                        break;
                    System.Threading.Thread.Sleep(50);
                }

                return _buffer.ToString();
            });
        }

        public async Task PeriodicMeasurement(bool enable)
        {
            _periodicMeasurement = enable;
            var cmd = enable ? "start" : "stop";
            if(enable)
                await SendAndWaitAsync(cmd);
            else
            {
                _client.Write(cmd);
                _buffer.Clear();
            }
        }


        public async Task<ReadingInfo> GetInformationAsync()
        {
            var result = await SendAndWaitAsync("read");

            var data = result.Split(',');

            ReadingInfo ri = new ReadingInfo
            {
                OverVoltageProtection = double.Parse(data[0].Trim().Substring(4), System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo),
                OverCurrentProtection = double.Parse(data[1].Trim().Substring(4), System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo),
                OverPowerProtection = double.Parse(data[2].Trim().Substring(4), System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo),
                LowVoltageProtection = double.Parse(data[3].Trim().Substring(4), System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo),
                MaximumCapacity = double.Parse(data[4].Trim().Substring(4), System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo),
                MaximumDischargeTime = TimeSpan.ParseExact(data[5].Trim().Substring(4), "hh\\:mm", null)
            };

            return ri;
        }
    }
}