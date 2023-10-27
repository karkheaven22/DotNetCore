using DotNetCore.Hubs;
using DotNetCore.Library.HttpLogging;
using LogHelper.Encryptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment WebHostEnvironment;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Replace |DataDirectory| placeholder in connection string
            string DatabaseConnection = Configuration.GetConnectionString("DefaultConnection").Replace("|DataDirectory|", Directory.GetCurrentDirectory());

            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseSqlServer(DatabaseConnection));

            services
                .AddIdentityCore<ApplicationUser>(options => Configuration.GetSection(nameof(IdentityOptions)).Bind(options))
                .AddRoles<ApplicationRole>()
                //.AddIdentity<ApplicationUser, ApplicationRole>(options => Configuration.GetSection(nameof(IdentityOptions)).Bind(options))
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>()
                .AddDefaultTokenProviders()
                .AddSignInManager();

            services.AddSingleton<IJwtFactory, JwtFactory>();
            services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
            services.AddScoped<ApiKeyAuthFilter>();

            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));
            var _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["JWT:SecurityKey"]));
            var ECDsaSigningKey = new ECDsaSecurityKey(AlgorithmECDsa.LoadECDsa(Configuration["JWT:ECPrivateKey"]));

            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                //options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
                options.SigningCredentials = new SigningCredentials(ECDsaSigningKey, SecurityAlgorithms.EcdsaSha256);
            });

            services.AddAuthentication(options =>{
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
               .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
               {
                   o.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                   o.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = false,
                       ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],
                       ValidateAudience = false,
                       ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       IssuerSigningKey = ECDsaSigningKey,
                       RequireExpirationTime = false,
                       ClockSkew = TimeSpan.Zero
                   };
                   o.SaveToken = true;
                   o.Events = new JwtBearerEvents
                   {
                       OnMessageReceived = context =>
                       {
                           var accessToken = context.Request.Query["access_token"];
                           // If the request is for our hub...
                           var path = context.HttpContext.Request.Path;
                           if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs/chat")))
                           {
                               context.Token = accessToken;
                           }
                           return Task.CompletedTask;
                       }
                   };
               })
               .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
               {
                   options.LoginPath = "/api/Auth/Login";
                   options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                   options.SlidingExpiration = true;
                   options.AccessDeniedPath = "/Forbidden/";
                   options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
               })
               //.AddOAuth("oauth", o =>
               //{
               //    o.AuthorizationEndpoint = "https://<Realm>.auth0.com/authorize?audience=resourceAPI-server";
               //    o.TokenEndpoint = "https://<REALM>.auth0.com/oauth/token";
               //    o.ClientId = "<clientID>";
               //    o.ClientSecret = "<secret>";
               //    o.CallbackPath = "/cb_oauth";
               //    o.SaveTokens = true;
               //})
               .AddIdentityCookies();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
            });

            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Formatting = WebHostEnvironment.IsDevelopment() ? Formatting.Indented : Formatting.None;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter() { NamingStrategy = new CamelCaseNamingStrategy() });
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                });

            services.AddSignalR().AddJsonProtocol();
            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

            services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DotNetCore", Version = "v1" });

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    }
                                },
                            new List<string>()
                        }
                    });
                });

            services.AddMvc();
            services.AddHttpContextAccessor();

            services.AddAntiforgery(options =>
            {
                options.SuppressXFrameOptionsHeader = true;
            });
            //services.AddResponseCaching();
            services.AddResponseCompression();
            services.AddDistributedMemoryCache();
            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.All;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IServiceProvider serviceProvider,
            ApplicationDbContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotNetCore v1"));

            app.UseHttpsRedirection();
            app.UseHsts();
            app.UseRouting();

            app.UseCors(builder =>
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            //.SetIsOriginAllowed((host) => true)


            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCookiePolicy();
            //app.UseResponseCaching();
            app.UseResponseCompression();
            app.UseStaticFiles();
            //app.UseHttpLogging();
            app.UseLogMiddleware();
            //app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                _ = context.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
                if (next != null)
                {
                    await next.Invoke();
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/hubs/chat");
            });
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            ContextSeed.SeedDataAsync(serviceProvider).Wait();
        }
    }

    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var id = await base.GenerateClaimsAsync(user);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            id.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
            id.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));
            return id;
        }
    }
}