using DAO_DbService.Contexts;
using Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DAO_DbService.Program;
using static Helpers.Constants.Enums;

namespace DAO_DbService
{
    public class Program
    {
        public class Settings
        {
            public string DbConnectionString { get; set; }
            public string LocalDbConnectionString { get; set; }
            public string RabbitMQUrl { get; set; }
            public string RabbitMQUsername { get; set; }
            public string RabbitMQPassword { get; set; }
            public string Voting_Engine_Url { get; set; }
            public string Service_Reputation_Url { get; set; }
            public string Service_Log_Url { get; set; }

            //DAO VARIABLES
            public string BidEscrowFormalQuorumRatio { get; set; }
            public string BidEscrowFormalVotingTime { get; set; }
            public string BidEscrowInformalQuorumRatio { get; set; }
            public string BidEscrowInformalVotingTime { get; set; }
            public string BidEscrowPaymentRatio { get; set; }
            public string BidEscrowWalletAddress { get; set; }
            public string DefaultPolicingRate { get; set; }
            public string DefaultReputationSlash { get; set; }
            public string DistributePaymentToNonVoters { get; set; }
            public string FiatConversionRateAddress { get; set; }
            public string FormalQuorumRatio { get; set; }
            public string FormalVotingTime { get; set; }
            public string ForumKycRequired { get; set; }
            public string InformalQuorumRatio { get; set; }
            public string InformalStakeReputation { get; set; }
            public string InformalVotingTime { get; set; }
            public string InternalAuctionTime { get; set; }
            public string PostJobDOSFee { get; set; }
            public string PublicAuctionTime { get; set; }
            public string ReputationConversionRate { get; set; }
            public string TimeBetweenInformalAndFormalVoting { get; set; }
            public string VABidAcceptanceTimeout { get; set; }
            public string VACanBidOnPublicAuction { get; set; }
            public string VotingClearnessDelta { get; set; }
            public string VotingIdsAddress { get; set; }
            public string VotingStartAfterJobSubmission { get; set; }

            //CHAIN SETTINGS
            public Blockchain? DaoBlockchain { get; set; }
        }

        public static Monitizer monitizer;
        public static Settings _settings { get; set; } = new Settings();
        public static Helpers.RabbitMQ rabbitMq = new Helpers.RabbitMQ();
        public static Mysql mysql = new Helpers.Mysql();
        public static DbContextOptions dbOptions;

        public static void Main(string[] args)
        {
                CreateHostBuilder(args).Build().Run();                
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
