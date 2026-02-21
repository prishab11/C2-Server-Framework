using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using TeamServer.Middleware;
using TeamServer.Services;

namespace TeamServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TeamServer", Version = "v1" });

                // This adds an "X-Api-Key" input box in the Swagger UI
                // so you can test authenticated endpoints easily
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "X-Api-Key",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Enter your API key"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            services.AddSingleton<IListenerService, ListenerService>();
            services.AddSingleton<IAgentService, AgentService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamServer v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Our API key check runs here — before any controller is reached.
            // IMPORTANT: This protects ALL routes including the listener routes.
            // We'll handle the agent exemption below.
            app.UseApiKey();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}