using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ModelSaber.API.Components;
using ModelSaber.Database;
using Newtonsoft.Json;

namespace ModelSaber.API
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
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", builder =>
                {
                    builder.AllowAnyHeader().WithMethods("GET", "POST").AllowAnyOrigin();
                });
            });
            services.AddDbContext<ModelSaberDbContext>(ServiceLifetime.Singleton);
            services.AddSingleton<ModelSaberSchema>();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new LongConverter());
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
            services.AddGraphQL((options, provider) =>
            {
                options.EnableMetrics = true;
                var logger = provider.GetRequiredService<ILogger<Startup>>();
                options.UnhandledExceptionDelegate =
                    ctx => logger.LogError($"{ctx.OriginalException.Message} occurred.");
            }).AddSystemTextJson().AddDataLoader().AddGraphTypes(typeof(ModelSaberSchema));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v3", new OpenApiInfo { Title = "ModelSaber_API", Version = "v3" });
                c.DocumentFilter<SwaggerAddEnumDescriptions>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v3/swagger.json", "ModelSaber API v3");
                    c.RoutePrefix = "";
                });
            }

            app.UseCors("DefaultPolicy");

            app.UseGraphQL<ModelSaberSchema>();
            app.UseGraphQLPlayground();

            app.UseHttpsRedirection();

            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "api/{controller}/{action=Index}/{id?}");
            });
        }
    }
}
