using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Discord_Stream_Bot_Backend
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
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.UseMemberCasing();
            });

            var hostUri = new Uri(Utility.ServerConfig.RedirectURI);
            services.AddCors(options =>
            {
                options.AddPolicy(name: "allowGET", builder =>
                {
                    builder.WithOrigins($"{hostUri.Scheme}://{hostUri.Authority}" )
                           .WithMethods("GET")
                           .WithHeaders("Content-Type");
                });
                options.AddPolicy(name: "allowPOST", builder =>
                {
                    builder.WithOrigins($"{hostUri.Scheme}://{hostUri.Authority}")
                           .WithMethods("POST")
                           .WithHeaders("Content-Type");
                });
            });

            // Add OpenAPI v3 document
            services.AddSwaggerDocument();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<Middleware.LogMiddleware>();
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseOpenApi();
            app.UseSwaggerUi3();
        }
    }
   
}
