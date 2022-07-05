 using System;
using System.Collections.Generic;
using System.Text;

namespace Helpers.Models.IdentityModels
{
    public class LoginChainModel
    {
        public string walletAddress { get; set; }
        public double reputation { get; set; }
        public Helpers.Constants.Enums.AppNames? application { get; set; }
        public string ip { get; set; } = "";
        public string port { get; set; } = "";
    }
}
