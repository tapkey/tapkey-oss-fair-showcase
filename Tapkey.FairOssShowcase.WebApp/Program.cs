using MudBlazor.Services;
using Tapkey.FairOssShowcase.Webapp;
using Tapkey.FairOssShowcase.Webapp.Authorization;
using Tapkey.FairOssShowcase.WebApp;
using Tapkey.FairOssShowcase.WebApp.Data;
using Tapkey.OssApiClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMemoryCache();

var appConfig = Utils.ParseAppConfig(builder.Configuration);

builder.Services.AddSingleton(appConfig);
builder.Services.AddScoped<OssApiHttpMessageHandler>();

builder.Services.AddHttpClient(AppConstants.OssApiClient).AddHttpMessageHandler<OssApiHttpMessageHandler>();

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ZendeskService>();
builder.Services.AddSingleton<SparkPostService>();
builder.Services.AddLogging();
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    var connectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(connectionString))
    {
        logging.AddApplicationInsights(
           configureTelemetryConfiguration: (config) => config.ConnectionString = connectionString,
           configureApplicationInsightsLoggerOptions: (options) => { }
        );
    }
});

builder.Services.AddMudServices();

var supportedCultures = new[] { "en", "de" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
