using BLL;
using DAL;
using DAL.Repositories;
using IdentityServer4.EntityFramework.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Security.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OB_BE_dotnet_auth
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
            X509Certificate2 cert = new X509Certificate2("example.pfx", Configuration.GetValue<string>("Certificate:Password"));
            string migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            //services.AddControllers();

            services.AddIdentityServer(options =>
            {
                options.Authentication.CookieAuthenticationScheme = "none";
                options.IssuerUri = Configuration.GetValue<string>("AuthorizationServer:Address");
            })
           .AddSigningCredential(cert)
           .AddResourceOwnerValidator<ResourceOwnerPasswordValidatorService>()
           .AddProfileService<UserProfileService>()
           .AddConfigurationStore(options =>
           {
               options.ConfigureDbContext = builder =>
                   builder.UseSqlServer(
                       Configuration.GetConnectionString("DefaultConnection"),
                       sql => sql.MigrationsAssembly(migrationsAssembly));
           })
           .AddOperationalStore(options =>
           {
               options.ConfigureDbContext = builder =>
                   builder.UseSqlServer(
                       Configuration.GetConnectionString("DefaultConnection"),
                       sql => sql.MigrationsAssembly(migrationsAssembly));
               options.EnableTokenCleanup = true;
               options.TokenCleanupInterval = 3600;
           })
           .AddPersistedGrantStore<PersistedGrantStore>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddCookie("none")
           .AddJwtBearer(options =>
           {
               options.Authority = Configuration.GetValue<string>("AuthorizationServer:Address");
               options.Audience = Configuration.GetValue<string>("Service:Name");
               options.RequireHttpsMetadata = false;
           });

            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddMvc();
            services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            );

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                     builder => builder.SetIsOriginAllowedToAllowWildcardSubdomains()
                         .WithOrigins("*")
                         .AllowAnyMethod()
                         .AllowAnyHeader()
                         .Build());
            });

            services.AddDbContextPool<OnBoardingAuthSkdDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds);
                    x.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                });
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
            services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sample Auth Service", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement(){
                {
                    new OpenApiSecurityScheme{
                        Reference = new OpenApiReference{
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                            },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                        },
                        new List<string>()
                        }
                });
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseIdentityServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sample v1");
            });

            InitConfig.InitializeDatabase(app);

        }
    }
}
