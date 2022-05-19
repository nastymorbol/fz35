using System;

namespace FZ35.Commands
{
    public class Fz35CommandFactory
    {
        public static IFz35Command OverVoltageProtection(double value)
        {
            return new Fz35SetOverVoltageProtection(value);
        }

        public static IFz35Command LoadCurrent(double load)
        {
            return new Fz35SetLoadCurrent(load);
        }

        public static IFz35Command SetLowVoltageProtection(double load)
        {
            return new SetLowVoltageProtection(load);
        }

        public static IFz35Command SetOverCurrentProtection(double load)
        {
            return new SetOverCurrentProtection(load);
        }

        public static IFz35Command SetOverPowerProtection(double load)
        {
            return new SetOverPowerProtection(load);
        }

        public static IFz35Command SetMaximumCapacity(double value)
        {
            return new SetMaximumCapacity(value);
        }

        public static IFz35Command SetMaximumDischargeTime(TimeSpan value)
        {
            return new SetMaximumDischargeTime(value);
        }
    }
}