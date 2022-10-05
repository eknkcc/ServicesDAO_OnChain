using System;

namespace Helpers.Models.DtoModels.MainDbDto
{
    public class ChainActionDto
    {
        public int ChainActionId { get; set; }
        public int UserId { get; set; }
        public string WalletAddress { get; set; }
        public string DeployJson { get; set; }
        public string Result { get; set; }
        public string DeployHash { get; set; }
        public string ActionType { get; set; }
        public string Status { get; set; }
        public DateTime CreateDate { get; set; }
    }
}