using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class Fz35SetOverVoltageProtection : IFz35Command
    {
        private double value;

        public Fz35SetOverVoltageProtection(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            // OVP:xx.x	S/F	Set Over Voltage Protection
            var cmd = $"OVP:{value.ToString("00.0", System.Globalization.NumberFormatInfo.InvariantInfo)}";
            return cmd;
        }
    }
}