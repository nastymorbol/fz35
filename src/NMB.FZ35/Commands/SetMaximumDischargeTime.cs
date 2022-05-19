using System;
using System.Security.Cryptography.X509Certificates;
namespace FZ35.Commands
{
    public class SetMaximumDischargeTime : IFz35Command
    {
        private TimeSpan value;

        public SetMaximumDischargeTime(TimeSpan value)
        {
            this.value = value;
        }

        public string Get()
        {
            // OHP:xx:xx
            var cmd = $"OHP:{value.ToString("hh\\:mm")}";

            return cmd;
        }
    }
}