namespace Helpers.Models.CasperServiceModels
{
    public class TotalReputation:ErrorResponse
    {
        public int? available_amount { get; set; }
        public int? staked_amount { get; set; }
    }
}