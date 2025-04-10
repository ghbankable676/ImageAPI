using ImageAPI.Models;
using ImageAPI.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ImageAPI.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

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