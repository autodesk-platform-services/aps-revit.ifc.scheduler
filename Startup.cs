/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Serilog;
using Newtonsoft.Json.Serialization;
using RevitToIfcScheduler.Context;
using RevitToIfcScheduler.Models;
using RevitToIfcScheduler.Utilities;

namespace RevitToIfcScheduler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppConfig.Services = services;
            AppConfig.ClientId = Configuration.GetValue<string>("ClientId");
            AppConfig.ClientSecret = Configuration.GetValue<string>("ClientSecret");
            if (string.IsNullOrEmpty(AppConfig.ClientId) || string.IsNullOrEmpty(AppConfig.ClientSecret))
            {
                throw new ApplicationException("Missing required app settings ClientId or ClientSecret.");
            }

            AppConfig.LogPath = Configuration.GetValue<string>("LogPath") ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppConfig.FilePath = Configuration.GetValue<string>("FilePath", "/Files");
            AppConfig.AppId = Configuration.GetValue<string>("AppId", "revit-to-ifc");
            AppConfig.SendGridApiKey = Configuration.GetValue<string>("SendGridApiKey");
            AppConfig.FromEmail = Configuration.GetValue<string>("FromEmail");
            AppConfig.ToEmail = Configuration.GetValue<string>("ToEmail");
            AppConfig.TwoLegScope = Configuration.GetValue<string>("TwoLegScope", "account:read data:read data:create data:write bucket:read bucket:create");
            AppConfig.ThreeLegScope = Configuration.GetValue<string>("ThreeLegScope", "user:read data:read data:create");
            AppConfig.IncludeShallowCopies = Configuration.GetValue<bool>("IncludeShallowCopies", true);
            AppConfig.ApsBaseUrl = Configuration.GetValue<string>("ApsBaseUrl", "https://developer.api.autodesk.com");
            AppConfig.SqlDB = Configuration.GetConnectionString("SqlDB");
            AppConfig.DataProtector = DataProtectionProvider.Create("RevitToIfc").CreateProtector("User");
            AppConfig.BucketKey = Configuration.GetValue<string>("BucketKey", $"{AppConfig.AppId}-{AppConfig.ClientId}".ToLower()).Substring(0, 35);

            AppConfig.AdminEmails = new List<string>();
            var adminEmails = Configuration.GetValue<string>("AdminEmails", "").Split(';').ToList();
            foreach (var email in adminEmails)
            {
                AppConfig.AdminEmails.Add(email.ToLower());
            }

            services.AddHangfire(config => config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(AppConfig.SqlDB, new SqlServerStorageOptions
                {
                    SlidingInvisibilityTimeout = TimeSpan.FromHours(2),
                }));

            services.AddHangfireServer();            
            
            services.AddDbContext<RevitIfcContext>(options =>
                options.UseSqlServer(
                    AppConfig.SqlDB,
                    b => b.MigrationsAssembly(typeof(RevitIfcContext).Assembly.GetName().ToString())));
            
            using(var dbContext = services.BuildServiceProvider().GetService<RevitIfcContext>())
            {
                dbContext.Database.EnsureCreated();
                //Run migration when EF models have been changed.
                //dbContext.Database.Migrate();
            }
            
            //Add service for accessing current HttpContext
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
            services.AddControllers();
            
            services.AddMvcCore().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
            
            Log.Information("Started Server");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHangfireServer(new BackgroundJobServerOptions()
                {
                    Queues = new[]{"default"},
                    WorkerCount = 5
                }
            );

            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                Authorization = new []{ new MyAuthorizationFilter() },
                AppPath = "/settings"
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

            BackgroundJob.Enqueue(()=>APS.CheckOrCreateTransientBucket(AppConfig.BucketKey));
        }
    }
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            ServiceProvider provider = (AppConfig.Services as ServiceCollection).BuildServiceProvider();
            Context.RevitIfcContext dbRevitIfcContext = provider.GetService<Context.RevitIfcContext>();
            
            return Authentication.IsAuthorized(context.GetHttpContext(), dbRevitIfcContext);
        }
    }
}