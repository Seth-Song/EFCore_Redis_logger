using EFCore_Redis_logger.Extension;
using EFCore_Redis_logger.Utility.ConfigurationHelper;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
ConfigurationHelper1 configureHelper = new ConfigurationHelper1(builder.Configuration);
IConfiguration config = new ConfigurationBuilder()
         .AddJsonFile("appsettings.json", true, true).Build();

ServiceCollectionExtensions.AddLogger(builder);


builder.Services.AddCacheService(configureHelper)
                .AddDBContext(configureHelper)
                .AddControllersWithViews();

// Add services to the container.


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
