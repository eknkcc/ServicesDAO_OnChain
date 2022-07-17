using System;
using System.Collections.Generic;
using System.Text;

namespace Helpers.Models.WebsiteViewModels
{
    public class UserChainProfile
    {
        public string Balance { get; set; } = "0";
        public string Reputation { get; set; } = "0";
        public bool IsVA { get; set; }
    }
}
