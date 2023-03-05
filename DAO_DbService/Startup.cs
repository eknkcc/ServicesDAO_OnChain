using Helpers;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;
using static DAO_DbService.Program;
using Microsoft.EntityFrameworkCore;
using DAO_DbService.Contexts;
using System.Threading;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace DAO_DbService
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            //Get related appsettings.json file (Development or Production)
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            LoadConfig(configuration);
            InitializeService();
        }

        /// <summary>
        ///  Loads application config from appsettings.json
        /// </summary>
        /// <param name="configuration"></param>
        public static void LoadConfig(IConfiguration configuration)
        {
            var config = configuration.GetSection("PlatformSettings");
            config.Bind(_settings);
        }

        /// <summary>
        ///  Initializes application (Db migrations, connection check, timer construction)
        /// </summary>
        public static void InitializeService()
        {
            monitizer = new Monitizer(_settings.RabbitMQUrl, _settings.RabbitMQUsername, _settings.RabbitMQPassword);

            ApplicationStartResult rabbitControl = rabbitMq.Initialize(_settings.RabbitMQUrl, _settings.RabbitMQUsername, _settings.RabbitMQPassword);
            if (!rabbitControl.Success)
            {
                monitizer.startSuccesful = -1;
                monitizer.AddException(rabbitControl.Exception, LogTypes.ApplicationError, true);
            }

            ApplicationStartResult mysqlMigrationcontrol = mysql.Migrate(new dao_maindb_context().Database);
            if (!mysqlMigrationcontrol.Success)
            {
                monitizer.startSuccesful = -1;
                monitizer.AddException(mysqlMigrationcontrol.Exception, LogTypes.ApplicationError, true);
            }

            ApplicationStartResult mysqlcontrol = mysql.Connect(_settings.DbConnectionString);
            if (!mysqlcontrol.Success)
            {
                monitizer.startSuccesful = -1;
                monitizer.AddException(mysqlcontrol.Exception, LogTypes.ApplicationError, true);
            }

            CreateDefaultSettings();

            TimedEvents.StartTimers();

            if (monitizer.startSuccesful != -1)
            {
                monitizer.startSuccesful = 1;
                monitizer.AddApplicationLog(LogTypes.ApplicationLog, monitizer.appName + " application started successfully.");
            }
        }

        public static void CreateDefaultSettings()
        {
            using (dao_maindb_context db = new dao_maindb_context())
            {
                var settings = db.DaoSettings.ToList();

                if (settings.Count(x => x.Key == "BidEscrowFormalQuorumRatio") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowFormalQuorumRatio", Value = Program._settings.BidEscrowFormalQuorumRatio, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "BidEscrowFormalVotingTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowFormalVotingTime", Value = Program._settings.BidEscrowFormalVotingTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "BidEscrowInformalQuorumRatio") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowInformalQuorumRatio", Value = Program._settings.BidEscrowInformalQuorumRatio, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "BidEscrowInformalVotingTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowInformalVotingTime", Value = Program._settings.BidEscrowInformalVotingTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "BidEscrowPaymentRatio") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowPaymentRatio", Value = Program._settings.BidEscrowPaymentRatio, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "BidEscrowWalletAddress") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "BidEscrowWalletAddress", Value = Program._settings.BidEscrowWalletAddress, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "DefaultPolicingRate") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "DefaultPolicingRate", Value = Program._settings.DefaultPolicingRate, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "DefaultReputationSlash") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "DefaultReputationSlash", Value = Program._settings.DefaultReputationSlash, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "DistributePaymentToNonVoters") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "DistributePaymentToNonVoters", Value = Program._settings.DistributePaymentToNonVoters, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "FiatConversionRateAddress") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "FiatConversionRateAddress", Value = Program._settings.FiatConversionRateAddress, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "FormalQuorumRatio") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "FormalQuorumRatio", Value = Program._settings.FormalQuorumRatio, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "FormalVotingTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "FormalVotingTime", Value = Program._settings.FormalVotingTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "ForumKycRequired") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "ForumKycRequired", Value = Program._settings.ForumKycRequired, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "InformalQuorumRatio") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "InformalQuorumRatio", Value = Program._settings.InformalQuorumRatio, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "InformalStakeReputation") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "InformalStakeReputation", Value = Program._settings.InformalStakeReputation, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "InformalVotingTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "InformalVotingTime", Value = Program._settings.InformalVotingTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "InternalAuctionTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "InternalAuctionTime", Value = Program._settings.InternalAuctionTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "PostJobDOSFee") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "PostJobDOSFee", Value = Program._settings.PostJobDOSFee, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "PublicAuctionTime") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "PublicAuctionTime", Value = Program._settings.PublicAuctionTime, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "ReputationConversionRate") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "ReputationConversionRate", Value = Program._settings.ReputationConversionRate, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "TimeBetweenInformalAndFormalVoting") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "TimeBetweenInformalAndFormalVoting", Value = Program._settings.TimeBetweenInformalAndFormalVoting, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "VABidAcceptanceTimeout") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "VABidAcceptanceTimeout", Value = Program._settings.VABidAcceptanceTimeout, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "VACanBidOnPublicAuction") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "VACanBidOnPublicAuction", Value = Program._settings.VACanBidOnPublicAuction, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "VotingClearnessDelta") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "VotingClearnessDelta", Value = Program._settings.VotingClearnessDelta, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "VotingIdsAddress") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "VotingIdsAddress", Value = Program._settings.VotingIdsAddress, LastModified = DateTime.Now });
                    db.SaveChanges();
                }

                if (settings.Count(x => x.Key == "VotingStartAfterJobSubmission") == 0)
                {
                    db.DaoSettings.Add(new Models.DaoSetting() { Key = "VotingStartAfterJobSubmission", Value = Program._settings.VotingStartAfterJobSubmission, LastModified = DateTime.Now });
                    db.SaveChanges();
                }
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDbContextPool<dao_maindb_context>(o => o.UseMySQL(_settings.DbConnectionString));            

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseStaticFiles();

            var defaultDateCulture = "en-US";
            var ci = new CultureInfo(defaultDateCulture);
            ci.NumberFormat.NumberDecimalSeparator = ".";
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            ci.NumberFormat.NumberGroupSeparator = ",";

            // Configure the Localization middleware
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(ci),
                SupportedCultures = new List<CultureInfo> { ci, },
                SupportedUICultures = new List<CultureInfo> { ci, }
            });

            DefaultFilesOptions DefaultFile = new DefaultFilesOptions();
            DefaultFile.DefaultFileNames.Clear();
            DefaultFile.DefaultFileNames.Add("Index.html");
            app.UseDefaultFiles(DefaultFile);
            app.UseStaticFiles();
        }
    }
}
