using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.Products;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this WebApplication app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products");

        products.MapGet("/", async (FlashSaleDbContext dbContext) =>
        {
            var productList = await dbContext.Products
                .AsNoTracking()
                .OrderBy(product => product.Name)
                .Select(product => ProductResponse.FromProduct(product))
                .ToArrayAsync();

            return Results.Ok(productList);
        })
        .WithName("GetProducts");

        products.MapGet("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var product = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == id)
                .Select(product => ProductResponse.FromProduct(product))
                .FirstOrDefaultAsync();

            return product is null ? Results.NotFound() : Results.Ok(product);
        })
        .WithName("GetProductById");

        products.MapPost("/", async (CreateProductRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = ProductRequestValidator.Validate(request.Name, request.Price);

            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var now = DateTimeOffset.UtcNow;
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                ImageUrl = request.ImageUrl.Trim(),
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            return Results.CreatedAtRoute(
                "GetProductById",
                new { id = product.Id },
                ProductResponse.FromProduct(product));
        })
        .WithName("CreateProduct");

        products.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = ProductRequestValidator.Validate(request.Name, request.Price);

            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var product = await dbContext.Products.FindAsync(id);

            if (product is null)
            {
                return Results.NotFound();
            }

            product.Name = request.Name.Trim();
            product.Description = request.Description.Trim();
            product.Price = request.Price;
            product.ImageUrl = request.ImageUrl.Trim();
            product.IsActive = request.IsActive;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(ProductResponse.FromProduct(product));
        })
        .WithName("UpdateProduct");

        products.MapDelete("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var product = await dbContext.Products.FindAsync(id);

            if (product is null)
            {
                return Results.NotFound();
            }

            dbContext.Products.Remove(product);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteProduct");

        return products;
    }
}

public static class ProductRequestValidator
{
    public static string? Validate(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (price <= 0)
        {
            return "Product price must be greater than 0.";
        }

        return null;
    }
}

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    bool IsActive = true);

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    bool IsActive);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductResponse FromProduct(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
