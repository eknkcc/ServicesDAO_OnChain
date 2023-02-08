using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteSlashingModel
    {
        public string address_to_slash { get; set; }
        public uint slash_ratio { get; set; }
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
    }
}
