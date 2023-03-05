using Helpers;
using Helpers.Models.DtoModels.MainDbDto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;

namespace DAO_WebPortal
{
    public class Program
    {
        public class Settings
        {
            //Folder containing the views. Different views can be shown for different organizations. For example for CRDAO Views-CRDAO folder can be used.
            //Default folder containing ServicesDAO content is "Views"
            public string ViewFolder { get; set; }

            //NETWORK VARIABLES
            public string RabbitMQUrl { get; set; }
            public string RabbitMQUsername { get; set; }
            public string RabbitMQPassword { get; set; }
            public string Service_ApiGateway_Url { get; set; }


            public string EncryptionKey { get; set; }

            //DAO Settings 
            public List<DaoSettingDto> DaoSettings { get; set; } = new List<DaoSettingDto>();

            //CHAIN SETTINGS
            public Blockchain? DaoBlockchain { get; set; }
        }

        public static Settings _settings { get; set; } = new Settings();
        public static Helpers.RabbitMQ rabbitMq = new Helpers.RabbitMQ();
        public static Monitizer monitizer;
        public static List<ChainActionDto> chainQue = new List<ChainActionDto>();

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
