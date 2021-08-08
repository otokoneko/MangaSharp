using MangaSharp.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace MangaSharp
{
    public class Startup
    {
        public Startup()
        {
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();

            services.AddScoped(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var appConfiguration = new AppConfiguration();
                configuration.Bind(appConfiguration);
                return appConfiguration;
            });

            services.AddScoped<ITranslator>(serviceProvider =>
            {
                var appConfig = serviceProvider.GetService<AppConfiguration>();
                var translatorConfig = appConfig.Translator;
                return translatorConfig.Default switch
                {
                    "Baidu" => new BaiduTranslator(translatorConfig.Baidu, serviceProvider.GetService<IHttpClientFactory>(), serviceProvider.GetService<ILogger<BaiduTranslator>>()),
                    "Caiyun" => new CaiyunTranslator(translatorConfig.Caiyun, serviceProvider.GetService<IHttpClientFactory>(), serviceProvider.GetService<ILogger<CaiyunTranslator>>()),
                    "Youdao" => new YoudaoTranslator(translatorConfig.Youdao, serviceProvider.GetService<IHttpClientFactory>(), serviceProvider.GetService<ILogger<YoudaoTranslator>>()),
                    _ => null,
                };
            });

            services.AddScoped<TextSegmentation>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
