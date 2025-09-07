using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Email.Config
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSSL { get; set; }
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Expense Tracker";
    }
}
