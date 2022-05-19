using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class Fz35SetLoadCurrent : IFz35Command
    {
        private double value;

        public Fz35SetLoadCurrent(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            // x.xxA	S/F	Set load current
            var cmd = $"{value.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)}A";
            return cmd;
        }
    }
}