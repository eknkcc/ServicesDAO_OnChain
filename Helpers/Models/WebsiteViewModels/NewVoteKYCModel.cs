using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteKYCModel
    {
        public string documentHash { get; set; }  //For KYC vote
        public string kycUserName { get; set; }  //For KYC vote
        public double stake { get; set; }
        public string signedDeployJson { get; set; }

    }
}
