using ImageAPI.Models;
using ImageAPI.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ImageAPI.Services;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()  
    .WriteTo.File(
        $"{logDirectory}/log-.log",   
        rollingInterval: RollingInterval.Day,  
        fileSizeLimitBytes: 10_000_000,       
        retainedFileCountLimit: 30           
    )
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Bind AppSettings section
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
builder.Services.AddSingleton(appSettings);

// Conditionally register the appropriate IImageRepository
if (appSettings.UseMongo)
{
    // Mongo repo and client
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
    builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
    builder.Services.AddScoped<IImageRepository, ImageRepositoryMongo>();
}
else
{
    // In-memory repo
    builder.Services.AddScoped<IImageRepository, ImageRepository>();
}

// Register services
builder.Services.AddScoped<ImageService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();