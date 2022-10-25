using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TightWiki
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
            services.AddRazorPages();
            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.LoginPath = "/Account/Login";

                        });

            services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30); //Login timeout.
            });

            /* https://khalidabuhakmeh.com/how-to-map-a-route-in-an-aspnet-core-mvc-application
             * First, since all controllers are built (newed up) by the service locator within ASP.NET Core,
             * we need to have the framework scan our project and register all Controller types. Registering
             * controllers is accomplished in the ConfigureServices method in our Startup class.
             */
            services.AddControllersWithViews();

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

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();

                /*
                Next, we�ll want to change how routes are registered. By default, there is a conventional route pattern.
                */
                /*
                endpoints.MapControllerRoute(name: "Default_Page_View",
                                pattern: "{pageNavigation}",
                                defaults: new { controller = "Page", action = "Display", pageNavigation = "Home" });

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Page}/{action=Display}/{pageNavigation?}");
                */

                endpoints.MapControllerRoute(
                    name: "File_Attachment",
                    pattern: "File/{action}/{pageNavigation}/{fileNavigation}",
                    defaults: new { controller = "File", action = "Binary", pageNavigation = "", fileNavigation = "", pageRevision = string.Empty }
                );

                endpoints.MapControllerRoute(
                    name: "File_Attachment_Revision",
                    pattern: "File/{action}/{pageNavigation}/{fileNavigation}/r/{pageRevision}",
                    defaults: new { controller = "File", action = "Binary", pageNavigation = "", fileNavigation = "", pageRevision = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Display",
                    pattern: "{pageNavigation}",
                    defaults: new { pageNavigation = "Home", controller = "Page", action = "Display", pageRevision = string.Empty }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Display_Revision",
                    pattern: "{pageNavigation}/r/{pageRevision}",
                    defaults: new { pageNavigation = "Home", controller = "Page", action = "Display", pageRevision = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Display_History",
                    pattern: "{pageNavigation}/History/{page}",
                    defaults: new { pageNavigation = "Home", controller = "Page", action = "History", pageRevision = string.Empty, page = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Revert_History",
                    pattern: "{pageNavigation}/Revert/{pageRevision}",
                    defaults: new { pageNavigation = "Home", controller = "Page", action = "Revert" }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Search",
                    pattern: "Page/Search/{page}",
                    defaults: new { controller = "Page", action = "Search", page = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "Admin_Moderate",
                    pattern: "Admin/Moderate/{page}",
                    defaults: new { controller = "Admin", action = "Moderate", page = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Edit",
                    pattern: "Page/Edit/{pageNavigation}",
                    defaults: new { controller = "Page", action = "Edit", pageNavigation = string.Empty }
                );

                endpoints.MapControllerRoute(
                    name: "Page_Default",
                    pattern: "Page/{action}/{pageNavigation}",
                    defaults: new { controller = "Page", action = "Display", pageNavigation = "Home" }
                );

                endpoints.MapControllerRoute(
                    name: "Tag_Associations",
                    pattern: "Tag/Browse/{pageNavigation}",
                    defaults: new { controller = "Tags", action = "Browse", pageNavigation = "Home" }
                );

                endpoints.MapControllerRoute(
                    name: "User_Avatar",
                    pattern: "User/{userAccountName}/Avatar",
                    defaults: new { controller = "User", action = "Avatar" }
                );

                endpoints.MapControllerRoute(
                    name: "User_Confirm",
                    pattern: "User/{userAccountName}/Confirm/{verificationCode}",
                    defaults: new { controller = "User", action = "Confirm" }
                );

                endpoints.MapControllerRoute(
                    name: "User_Reset",
                    pattern: "User/{userAccountName}/Reset/{verificationCode}",
                    defaults: new { controller = "User", action = "Reset" }
                );

                endpoints.MapControllerRoute(
                    name: "Admin_Account",
                    pattern: "Admin/Account/{navigation}",
                    defaults: new { controller = "Admin", action = "Account", navigation = "admin" }
                );

                endpoints.MapControllerRoute(
                    name: "Admin_Generic",
                    pattern: "Admin/{action}/{page}",
                    defaults: new { controller = "Admin", action = "Config", page = 1 }
                );

                endpoints.MapControllerRoute(
                    name: "DefaultOther",
                    pattern: "{controller}/{action}",
                    defaults: new { controller = "Page", action = "Login" }
                );

                //endpoints.MapControllers();
            });

            Shared.ADO.Singletons.ConnectionString = ConfigurationExtensions.GetConnectionString(this.Configuration, "TightWikiADO");

        }
    }
}