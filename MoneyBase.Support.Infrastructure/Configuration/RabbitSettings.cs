using System;

namespace MoneyBase.Support.Infrastructure.Configuration
{
    public class RabbitSettings
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string Queue { get; set; }
    }
}
