using DddApi.Context;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using DddApi.RabbitMq;
using DddApi.Models;
using DddApi.Services;
using DddApi.Configurations;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DddDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("regiao", httpclient => {
    httpclient.BaseAddress = new Uri("http://localhost:5006/");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "DddApi";
    config.Title = "DddApi v1";
    config.Version = "v1";
});

builder.Services.AddHostedService<RegiaoHostedService>();
builder.Services.AddSingleton<IRegiaoService, RegiaoService>();
builder.Services.AddSingleton<IMessageProducer>(provider => new Producer("ddd.updated"));

var app = builder.Build();

app.UsePrometheusMetrics();

var consumer = new RabbitMqConsumer<Regiao>("localhost", "regiao.updated", app.Services.GetRequiredService<IRegiaoService>());
Task.Run(() => consumer.StartConsumer());

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "DddAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DddDb>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}


app.MapDddEndpoints();
app.MapGet("/regioes", (IRegiaoService regiaoService) =>
{
    
    return TypedResults.Ok(regiaoService.GetCachedRegioes());
});

app.MapMetrics();
app.Run();

public partial class Program { }