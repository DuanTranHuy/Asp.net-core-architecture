using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OA.Domain.Auth;
using OA.Domain.Common;
using OA.Domain.Settings;
using OA.Persistence;
using OA.Service.Contract;
using OA.Service.Implementation;
using System;
using System.Reflection;
using System.Text;
using Npgsql.EntityFrameworkCore;

namespace OA.Service
{
    public static class DependencyInjection
    {
        public static void AddServiceLayer(this IServiceCollection services)
        {
            // or you can use assembly in Extension method in Infra layer with below command
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient<IEmailService, MailService>();
        }

        public static void AddIdentityService(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<IdentityContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityConnection"),
                b => b.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName)));

            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();
            #region Services
            services.AddTransient<IAccountService, AccountService>();
            #endregion
            services.Configure<JWTSettings>(configuration.GetSection("JWTSettings"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                  {
                      options.Authority = "https://localhost:5001";

                      options.TokenValidationParameters = new TokenValidationParameters
                      {
                          ValidateAudience = false
                      };
                      options.Events = new JwtBearerEvents()
                      {
                          OnAuthenticationFailed = c =>
                          {
                              c.NoResult();
                              c.Response.StatusCode = 500;
                              c.Response.ContentType = "text/plain";
                              return c.Response.WriteAsync(c.Exception.ToString());
                          },
                          OnChallenge = context =>
                          {
                              context.HandleResponse();
                              context.Response.StatusCode = 401;
                              context.Response.ContentType = "application/json";
                              var result = JsonConvert.SerializeObject(new Response<string>("You are not Authorized"));
                              return context.Response.WriteAsync(result);
                          },
                          OnForbidden = context =>
                          {
                              context.Response.StatusCode = 403;
                              context.Response.ContentType = "application/json";
                              var result = JsonConvert.SerializeObject(new Response<string>("You are not authorized to access this resource"));
                              return context.Response.WriteAsync(result);
                          },
                      };
                  });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "scope1");
                });
            });
        }
    }
}

