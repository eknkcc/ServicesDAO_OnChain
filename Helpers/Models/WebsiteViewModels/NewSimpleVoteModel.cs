using System;
using System.Collections.Generic;
using System.Text;
using Helpers.Models.DtoModels.MainDbDto;
using static Helpers.Constants.Enums;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewSimpleVoteModel
    {
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string vausername { get; set; } //For VA vote
        public string documentHash { get; set; }  //For KYC vote
        public string kycUserName { get; set; }  //For KYC vote
        public PlatformSettingDto settings { get; set; } //For variable repository (governance) vote
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
    }
}
