using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteSlashingModel
    {
        public string addresstoslash { get; set; }
        public uint slashratio { get; set; }
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
    }
}
