using System;

namespace FZ35
{
    public struct ReadingInfo
    {
        public double LowVoltageProtection { get; internal set; }
        public double OverVoltageProtection { get; internal set; }
        public double OverCurrentProtection { get; internal set; }
        public double OverPowerProtection { get; internal set; }
        public double MaximumCapacity { get; internal set; }
        public TimeSpan MaximumDischargeTime { get; internal set; }

        public override string ToString()
        {
            return $"LVP: {LowVoltageProtection:0.00}, OVP: {OverVoltageProtection:0.00}, OCP: {OverCurrentProtection:0.00}, OPP: {OverPowerProtection:0.00}, OAH: {MaximumCapacity:0.000}, OHP: {MaximumDischargeTime}";
        }
    }
}