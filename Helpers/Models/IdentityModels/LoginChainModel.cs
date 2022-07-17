 using System;
using System.Collections.Generic;
using System.Text;

namespace Helpers.Models.IdentityModels
{
    public class LoginChainModel
    {
        public string walletAddress { get; set; }
        public bool isVA { get; set; }
        public Helpers.Constants.Enums.AppNames? application { get; set; }
        public string ip { get; set; } = "";
        public string port { get; set; } = "";
    }
}
