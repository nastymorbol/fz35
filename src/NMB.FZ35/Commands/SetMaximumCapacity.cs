using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class SetMaximumCapacity : IFz35Command
    {
        private double value;

        public SetMaximumCapacity(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            // OAH
            var cmd = $"OAH:{value.ToString("0.000", System.Globalization.NumberFormatInfo.InvariantInfo)}";

            return cmd;
        }
    }
}