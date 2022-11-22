namespace Helpers.Models.CasperServiceModels
{
    public class AggregatedReputationChange
    {
        public string address { get; set; }
        public int earned_amount { get; set; }
        public int lost_amount { get; set; }
        public int released_amount { get; set; }
        public int staked_amount { get; set; }
        public string timestamp { get; set; }
        public int voting_id { get; set; }
    }
}