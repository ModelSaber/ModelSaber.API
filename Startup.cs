using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.Validation.Complexity;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ModelSaber.API.GraphQL;
using ModelSaber.Database;
using ModelSaber.API.Helpers;
using Prometheus;
using GraphQLServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace ModelSaber.API
{
    public class Startup
    {
        private const long CacheSize = 10 * 1024 * 1024;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", builder =>
                {
                    builder.AllowAnyHeader().WithMethods("GET", "POST").AllowAnyOrigin();
                });
            });
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddDbContext<ModelSaberDbContext>();
            services.AddSingleton<ModelSaberDbContextLeaser>();
            services.AddSingleton<ModelSaberSchema>();
            services.AddSingleton(_ => new MemoryDocumentCache(new MemoryDocumentCacheOptions
            {
                SizeLimit = CacheSize,
                SlidingExpiration = new TimeSpan(0, 1, 0, 0),
                ExpirationScanFrequency = new TimeSpan(0, 0, 10, 0)
            }));
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.AddRange(JsonConverters.Converters);
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            services.AddGraphQL(builder =>
                {
                    builder
                        .AddSchema<ModelSaberSchema>()
                        .ConfigureExecution((options, next) =>
                        {
                            options.EnableMetrics = true;
                            var logger = options.RequestServices?.GetRequiredService<ILogger<Startup>>();
                            options.UnhandledExceptionDelegate = ctx => {
                                logger?.LogError("{Error}\n{Stack}", ctx.OriginalException.Message, ctx.OriginalException.StackTrace);
                                return Task.CompletedTask;
                            };
                            return next(options);
                        })
                        .AddSystemTextJson(options =>
                        {
                            options.Converters.AddRange(JsonConverters.Converters);
                        })
                        .AddErrorInfoProvider<ErrorInfoProvider>() // TODO change this to custom error provider later https://github.com/graphql-dotnet/server/blob/develop/samples/Samples.Server/CustomErrorInfoProvider.cs
                        //.AddWebSockets() // TODO update events through websocket how ever the fuck we are going to do that idk yet
                        .AddDocumentCache<MemoryDocumentCache>()
                        .AddGraphTypes(typeof(ModelSaberSchema).Assembly)
                        .AddUserContextBuilder<UserContextBuilder>()
                        .AddValidationRule<AuthValidationRule>()
                        .AddMiddleware<MetricsFieldMiddleware>(true, GraphQLServiceLifetime.Singleton)
                        .AddComplexityAnalyzer<ComplexityAnalyzer>()
                        .AddHttpMiddleware<ModelSaberSchema>();
                })
                .Configure<ErrorInfoProviderOptions>(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment());
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v3", new OpenApiInfo
                {
                    Title = "ModelSaber.API",
                    Version = "v3",
                    Description = ""
                });
                c.DocumentFilter<SwaggerAddEnumDescriptions>();
            });
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.SmallestSize;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v3/swagger.json", "ModelSaber API v3");
                c.RoutePrefix = "";
            });

            app.UseCors("DefaultPolicy");

            app.UseGraphQL<ModelSaberSchema>(); // TODO possibly extend this to use https://github.com/graphql-dotnet/server/blob/develop/samples/Samples.Server/GraphQLHttpMiddlewareWithLogs.cs as a base for extending logging on GQL
            app.UseGraphQLPlayground("/gqlplayground");
            app.UseGraphQLVoyager("/voyager");

            app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseRouting();

            app.UseHttpMetrics();
            app.UseMetricServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "api/{controller}/{action=Index}/{id?}");
            });

        }
    }
}
