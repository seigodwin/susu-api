using Susu.Infrastructure.Extensions;
using Susu.Application.Services;
using Susu.Application.Interfaces;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Infrastructure and application services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IGroupService, GroupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
