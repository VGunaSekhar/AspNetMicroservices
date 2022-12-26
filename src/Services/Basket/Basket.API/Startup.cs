using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Basket.API.Repositories;
using Basket.API.GrpcServices;
using Discount.Grpc.Protos;
using MassTransit;

namespace Basket.API
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
            services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(
                o =>o.Address = new Uri(Configuration["GrpcSettings:DiscountUrl"]));
            services.AddScoped<DiscountGrpcService>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetValue<string>("CacheSettings:ConnectionString"); 
            });
            services.AddScoped<IBasketRepository, BasketRepository>();
            services.AddAutoMapper(typeof(Startup));
            services.AddControllers();
            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1",
                    new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "Basket.API",
                        Description = "",
                        Version = "v1 "
                    }
                        );
            });
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
            // MassTransit-RabbitMQ Configuration
            services.AddMassTransit(config => {
                config.UsingRabbitMq((ctx, cfg) => {
                    cfg.Host(Configuration["EventBusSettings:HostAddress"]);
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

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket.API");
            });
        }
    }
}
