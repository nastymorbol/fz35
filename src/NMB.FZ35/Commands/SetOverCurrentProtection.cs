using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class SetOverCurrentProtection : IFz35Command
    {
        private double value;

        public SetOverCurrentProtection(double value)
        {
            this.value = value;
        }

        public string Get()
        {
            var cmd = $"OCP:{value.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)}";

            return cmd;
        }
    }
}