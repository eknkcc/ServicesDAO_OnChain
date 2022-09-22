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
        public string vausername { get; set; }
        public string documentHash { get; set; }
        public string kycUserName { get; set; }
        public PlatformSettingDto settings { get; set; }
    }


}
