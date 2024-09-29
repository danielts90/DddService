using DddApi.Context;
using DddApi.Entities;
using DddApi.Models;
using DddApi.RabbitMq;
using DddApi.Services;
using DDdApi.Models;
using Microsoft.EntityFrameworkCore;

public static class DddEndpoints 
{
    public static void MapDddEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/ddd")
            .MapEndpoints()
            .WithTags("Ddd");
    }

public static async Task<IResult> HandleGetAll(DddDb db)
{
    return TypedResults.Ok(await db.Ddds.ToArrayAsync());
}
public static async Task<IResult> HandleGetById(IRegiaoService regiaoService, int id, DddDb db)
{
    DddDto ddd = await db.Ddds.FindAsync(id);
    var regioes = regiaoService.GetCachedRegioes();
    ddd.Regiao = regioes.FirstOrDefault(o => o.Id == ddd.RegiaoId);
    return  TypedResults.Ok(ddd);
             
}
public static async Task<IResult> HandleCreate(Ddd ddd, DddDb db, IMessageProducer producer)
{
    db.Ddds.Add(ddd);
    await db.SaveChangesAsync();

    var message = new Message<Ddd>(EventTypes.CREATE, ddd);
    producer.SendMessageToQueue(message);

    return TypedResults.Created($"/ddd/{ddd.Id}", ddd);
}
public static async Task<IResult> HandleUpdate(int id, Ddd inputDdd, DddDb db, IMessageProducer producer)
{
    var ddd = await db.Ddds.FindAsync(id);

    if (ddd is null) return TypedResults.NotFound();

    ddd.Code = inputDdd.Code;
    await db.SaveChangesAsync();

    var message = new Message<Ddd>(EventTypes.UPDATE, ddd);
    producer.SendMessageToQueue(message);

    return TypedResults.NoContent();
}
public static async Task<IResult> HandleDelete(int id, DddDb db, IMessageProducer producer)
{
    if (await db.Ddds.FindAsync(id) is Ddd ddd)
    {
        db.Ddds.Remove(ddd);
        await db.SaveChangesAsync();

        var message = new Message<Ddd>(EventTypes.DELETE, ddd);
        producer.SendMessageToQueue(message);

        return TypedResults.NoContent();
    }
    return TypedResults.NotFound();
}
private static RouteGroupBuilder MapEndpoints(this RouteGroupBuilder group)
{
    group.MapGet("/", HandleGetAll);
    group.MapGet("/{id}", HandleGetById);
    group.MapPost("/", HandleCreate);
    group.MapPut("/{id}", HandleUpdate);
    group.MapDelete("/{id}", HandleDelete);
    return group;
 }
}