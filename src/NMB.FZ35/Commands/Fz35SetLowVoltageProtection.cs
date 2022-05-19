using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class SetLowVoltageProtection : IFz35Command
    {
        private double value;

        public SetLowVoltageProtection(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            var cmd = $"LVP:{value.ToString("00.0", System.Globalization.NumberFormatInfo.InvariantInfo)}";
            return cmd;
        }
    }
}