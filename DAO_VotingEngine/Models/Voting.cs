﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;

namespace DAO_VotingEngine.Models
{
    public class Voting
    {
        [Key]
        public int VotingID { get; set; }
        public int JobID { get; set; }
        public bool IsFormal { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public VoteStatusTypes Status { get; set; }
        public int? QuorumCount { get; set; }
        public int? EligibleUserCount { get; set; }
        public double? QuorumRatio { get; set; }
        public VoteTypes Type { get; set; }
        public int VoteCount { get; set; }
        public double? StakedFor { get; set; }
        public double? StakedAgainst { get; set; }

        //How much of the new minted reputation will be distributed to job doer.
        public double PolicingRate { get; set; }
        public string DeployHash { get; set; }
        public int? BlockchainVotingID { get; set; }
    }


}
