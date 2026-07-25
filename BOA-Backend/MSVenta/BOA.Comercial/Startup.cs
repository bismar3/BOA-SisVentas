using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using BOA.Comercial.Repositories;
using BOA.Comercial.Services;

namespace BOA.Comercial
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BOA.Comercial", Version = "v1" });
            });

            var cn = Configuration["mysql:cn"];
            services.AddDbContext<ContextDatabase>(options =>
                options.UseMySql(cn, ServerVersion.AutoDetect(cn))
            );

            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IVentaService, VentaService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<ITransaccionService, TransaccionService>();
            services.AddScoped<IReservaService, ReservaService>();
            services.AddScoped<IBitacoraService, BitacoraService>();

            services.AddHostedService<RabbitMQConsumer>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BOA.Comercial v1"));
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}