using Helpers;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DAO_WebPortal.Program;
using static Helpers.Constants.Enums;
using System.Timers;

namespace DAO_WebPortal
{
    public class Startup
    {
        public static System.Timers.Timer daoSettingsTimer;
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

            //DAO settings refresh timer
            daoSettingsTimer = new System.Timers.Timer(60000);
            daoSettingsTimer.Elapsed += LoadDaoSettings;
            daoSettingsTimer.AutoReset = true;
            daoSettingsTimer.Enabled = true;
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
            Helpers.Encryption.EncryptionKey = Program._settings.EncryptionKey;

            monitizer = new Monitizer(_settings.RabbitMQUrl, _settings.RabbitMQUsername, _settings.RabbitMQPassword);

            LoadDaoSettings(null, null);

            ApplicationStartResult rabbitControl = rabbitMq.Initialize(_settings.RabbitMQUrl, _settings.RabbitMQUsername, _settings.RabbitMQPassword);
            if (!rabbitControl.Success)
            {
                monitizer.startSuccesful = -1;
                monitizer.AddException(rabbitControl.Exception, LogTypes.ApplicationError, true);
            }

            if (monitizer.startSuccesful != -1)
            {
                monitizer.startSuccesful = 1;
                monitizer.AddApplicationLog(LogTypes.ApplicationLog, monitizer.appName + " application started successfully.");
            }
        }

        /// <summary>
        ///  Loads platform settings from db if exists.
        /// </summary>
        public static void LoadDaoSettings(Object source, ElapsedEventArgs e)
        {
            //Ensure db service is up and running
            Thread.Sleep(10000);

            try
            {
                //Get latest platform settings (DAO variables from db)
                string settingsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/PublicActions/GetAllSettings");
                //If custom settings found
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    List<DaoSettingDto> settings = Serializers.DeserializeJson<List<DaoSettingDto>>(settingsJson);
                    if (settings.Count > 0)
                    {
                        Program._settings.DaoSettings = settings;
                    }
                    else
                    {
                        monitizer.startSuccesful = -1;
                    }
                }
                else
                {
                    monitizer.startSuccesful = -1;
                }
            }
            catch(Exception ex)
            {
                monitizer.AddException(ex, LogTypes.ApplicationError, true);
                monitizer.startSuccesful = -1;
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Changes default Views folder
            if (!string.IsNullOrEmpty(Program._settings.ViewFolder))
            {
                services.Configure<RazorViewEngineOptions>(o =>
                {
                    o.ViewLocationFormats.Clear();
                    o.ViewLocationFormats.Add("/" + Program._settings.ViewFolder + "/{1}/{0}" + RazorViewEngine.ViewExtension);
                    o.ViewLocationFormats.Add("/" + Program._settings.ViewFolder + "/Shared/{0}" + RazorViewEngine.ViewExtension);
                });
            }

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLocalization(o => o.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(opts =>
            {
                var supportedCultures = new[] { new CultureInfo("en-GB") };
                opts.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR");
                opts.SupportedCultures = supportedCultures;
                opts.SupportedUICultures = supportedCultures;

            });

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    options.HttpsPort = 443;
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/Error");

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.UseStaticFiles();

            //app.UseHttpsRedirection();

            app.UseHsts();

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Public}/{action=Index}/{id?}");
            });
        }
    }
}
