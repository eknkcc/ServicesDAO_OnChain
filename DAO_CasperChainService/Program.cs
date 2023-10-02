using Helpers;
using Helpers.Models.DtoModels.LogDbDto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAO_CasperChainService
{
    public class Program
    {
        public class Settings
        {
            public string DbConnectionString { get; set; }
            public string RabbitMQUrl { get; set; }
            public string RabbitMQUsername { get; set; }
            public string RabbitMQPassword { get; set; }
            public string NodeUrl { get; set; }
            public string ChainName { get; set; }
            public string CasperMiddlewareUrl { get; set; }

            //CONTRACTS ADDRESSES
            public string VariableRepositoryContract { get; set; }
            public string ReputationContract { get; set; }
            public string DaoIdsContract { get; set; }
            public string VaNftContract { get; set; }
            public string KYCVoterContract { get; set; }
            public string KycNftContract { get; set; }
            public string SlashingVoterContract { get; set; }
            public string SimpleVoterContract { get; set; }
            public string ReputationVoterContract { get; set; }
            public string RepoVoterContract { get; set; }
            public string AdminContract { get; set; }
            public string OnboardingRequestContract { get; set; }
            public string KycVoterContract { get; set; }
            public string BidEscrowContract { get; set; }
            public string VAOnboardingPackageHash { get; set; }
            public string BidEscrowContractPackageHash { get; set; }
            public string RepoVoterContractPackageHash { get; set; }
            public string VariableRepositoryPackageHash { get; set; }
        }

        public static Monitizer monitizer;
        public static Settings _settings { get; set; } = new Settings();
        public static Helpers.RabbitMQ rabbitMq = new Helpers.RabbitMQ();
        public static Mysql mysql = new Helpers.Mysql();
        public static DbContextOptions dbOptions;

        public static ConcurrentQueue<UserLogDto> UserLogs = new ConcurrentQueue<UserLogDto>();
        public static ConcurrentQueue<ApplicationLogDto> ApplicationLogs = new ConcurrentQueue<ApplicationLogDto>();
        public static ConcurrentQueue<ErrorLogDto> ErrorLogs = new ConcurrentQueue<ErrorLogDto>();

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
