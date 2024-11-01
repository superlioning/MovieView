using Microsoft.EntityFrameworkCore;
using Amazon;
using Microsoft.Data.SqlClient;
using _301230968_Zhang__Lab03.Connector;
using _301230968_Zhang__Lab03.Models;
using _301230968_Zhang__Lab03.Repository;
using _301230968_Zhang__Lab03.Service;

namespace _301230968_Zhang__Lab03
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Set up detailed logging
            builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Trace);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(36000);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // DB
            //builder.Configuration.AddSystemsManager("/APIEngineeringLab03", new Amazon.Extensions.NETCore.Setup.AWSOptions
            //{
            //    Region = RegionEndpoint.CACentral1
            //});

            var connectionString = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection2RDS"));
            //connectionString.UserID = builder.Configuration["DbUser"];
            //connectionString.Password = builder.Configuration["DbPassword"];
            builder.Services.AddDbContext<MovieContext>(options => options.UseSqlServer(connectionString.ConnectionString));

            // Register repository and service 
            builder.Services.AddScoped<MovieRepository>();
            builder.Services.AddScoped<MovieService>();

            // Register AWSConnector service here
            builder.Services.AddSingleton<AWSConnector>(sp => new AWSConnector(sp.GetRequiredService<IConfiguration>()));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}