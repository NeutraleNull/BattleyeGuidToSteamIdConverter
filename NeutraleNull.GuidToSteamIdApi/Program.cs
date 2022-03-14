using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NeutraleNull.GuidToSteamIdApi.Database;
using NeutraleNull.GuidToSteamIdApi.UseCases;
using NeutraleNull.GuidToSteamIdApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql("server=localhost;port=3308;database=test;user=dragon;password=***", new MariaDbServerVersion(new Version(10, 5, 12))));
builder.Services.AddTransient<ISeedDatabaseUseCase, SeedDatabaseUseCase>();
builder.Services.AddHostedService<DataGenerationService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("http://localhost:5004",
                                "https://guidtosteamid.neutralenull.de");
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();

app.UseCors();

app.MapGet("/GetSteamId/{guid}", async ([FromRoute] string guid, [FromServices] ApplicationDbContext db, [FromServices] IMemoryCache memoryCache) =>
{
    if (memoryCache.TryGetValue(guid, out var cachedResult))
    {
        return Results.Ok(cachedResult);
    }

    var res = await db.BattleyeGuidSteamIdLookupTable.FindAsync(guid);

    if (res is null) return Results.NotFound();

    var entry = memoryCache.CreateEntry(guid);
    entry.SetValue(res?.SteamId64);

    return Results.Ok(res);
}).Produces<BattleyeGuidSteamIdTuple>(StatusCodes.Status200OK)
   .Produces(StatusCodes.Status404NotFound); ;

app.Run("http://127.0.0.1:5004");

