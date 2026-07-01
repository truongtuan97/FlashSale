using FlashSale.Api.FlashSaleCampaigns;
using FlashSale.Api.FlashSaleItems;
using FlashSale.Api.Products;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<FlashSaleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapProductEndpoints();
app.MapFlashSaleCampaignEndpoints();
app.MapFlashSaleItemEndpoints();

app.Run();

public partial class Program;
