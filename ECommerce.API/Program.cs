using Asp.Versioning;
using ECommerce.API;
using ECommerce.API.Endpoints;
using ECommerce.API.Health;
using ECommerce.API.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding;
using ECommerce.UseCases;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Products", policy =>
    {
        // Every query-string field the spec reads has to vary the key, otherwise
        // two different searches or sorts collide on the same cached page.
        // Tagged so catalog writes can evict every cached page at once.
        policy.Expire(TimeSpan.FromMinutes(1))
        .Tag(OutputCacheTags.Products)
        .SetVaryByQuery(
            "pageNumber", "pageSize", "search",
            "brandId", "typeId", "sortBy", "sortDescending");
    });

    options.AddPolicy("DeliveryMethods", policy =>
    {
        policy.Expire(TimeSpan.FromMinutes(10));
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Before UseOutputCache: the cache must never be able to serve a stored response
// to a request whose identity has not been established yet.
app.UseAuthentication();
app.UseAuthorization();

// After routing (implicit in minimal APIs) so endpoint-level policies resolve.
app.UseRateLimiter();

app.UseOutputCache();

// Liveness: the process is up and serving requests. No dependency checks, so a
// database outage does not get a healthy container restarted for nothing.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: PostgreSQL (and Redis when configured) are reachable.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
});

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapProductEndpoints(apiVersionSet);
app.MapTypeEndpoints(apiVersionSet);
app.MapBrandEndpoints(apiVersionSet);
app.MapBasketEndpoints(apiVersionSet);
app.MapUserEndpoints(apiVersionSet);
app.MapDeliveryMethodEndPoints(apiVersionSet);
app.MapOrderEndpoints(apiVersionSet);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // runs middleware to make the openapi doc available to use.
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbseed = scope.ServiceProvider.GetRequiredService<DataBaseSeeder>();

    var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

    var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityStoreDbContext>();

    // Both contexts share a database but keep separate migration histories,
    // so each one has to be migrated before any seeder touches it.
    await dbContext.Database.MigrateAsync();

    await identityDbContext.Database.MigrateAsync();

    await dbseed.SeedAll();
}

app.Run();
