using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteReputationModel
    {
        public string signedDeployJson { get; set; }
        public int action { get; set; }
        public string subjectaddress { get; set; }
        public int amount { get; set; }
        public string documenthash { get; set; }
        public int stake { get; set; }
    }
}
