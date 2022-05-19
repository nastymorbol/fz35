using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class SetOverPowerProtection : IFz35Command
    {
        private double value;

        public SetOverPowerProtection(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            // OPP
            var cmd = $"OPP:{value.ToString("00.00", System.Globalization.NumberFormatInfo.InvariantInfo)}";

            return cmd;
        }
    }
}