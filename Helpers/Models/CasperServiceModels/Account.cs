using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.CasperServiceModels
{
    public class Account
    {
        public string hash { get; set; }
        public bool is_kyc { get; set; }
        public bool is_va { get; set; }
        public string timestamp { get; set; }

    }
}
