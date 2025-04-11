using ImageAPI.Models;
using ImageAPI.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ImageAPI.Services;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load config
var configuration = builder.Configuration;

// Check if file logging is enabled
bool enableFileLogging = configuration.GetSection("Serilog").GetValue<bool>("EnableFileLogging");
string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

// Create Serilog logger
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);

if (enableFileLogging)
{
    var fileSettings = configuration.GetSection("Serilog:FileLogging");
    var logPath = fileSettings.GetValue<string>("Path") ?? $"{logDirectory}/log-.log";
    var rolling = fileSettings.GetValue("RollingInterval", RollingInterval.Day);
    var sizeLimit = fileSettings.GetValue("FileSizeLimitBytes", 10_000_000);
    var retained = fileSettings.GetValue("RetainedFileCountLimit", 30);

    loggerConfig = loggerConfig.WriteTo.File(
        path: logPath,
        rollingInterval: rolling,
        fileSizeLimitBytes: sizeLimit,
        retainedFileCountLimit: retained
    );
}

Log.Logger = loggerConfig.CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Bind AppSettings section
builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
builder.Services.AddSingleton(appSettings);

// Conditionally register the appropriate IImageRepository
if (appSettings.UseMongo)
{
    var mongoConnectionString = configuration.GetConnectionString("MongoDb");
    builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
    builder.Services.AddScoped<IImageRepository, ImageRepositoryMongo>();
}
else
{
    builder.Services.AddScoped<IImageRepository, ImageRepository>();
}

builder.Services.AddScoped<ImageService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();