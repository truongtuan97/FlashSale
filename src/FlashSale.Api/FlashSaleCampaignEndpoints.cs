using FlashSale.Infrastructure.Persistence;
using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.FlashSaleCampaigns;

public static class FlashSaleCampaignEndpoints
{
    public static RouteGroupBuilder MapFlashSaleCampaignEndpoints(this WebApplication app)
    {
        var flashSaleCampaigns = app.MapGroup("/api/flash-sale-campaigns").WithTags("Flash Sale Campaigns");

        flashSaleCampaigns.MapGet("/", async (FlashSaleDbContext dbContext) =>
        {
            var flashSaleCampaignList = await dbContext.FlashSaleCampaigns
                .AsNoTracking()
                .OrderBy(flashSaleCampaign => flashSaleCampaign.Name)
                .Select(flashSaleCampaign => FlashSaleCampaignResponse.FromFlashSaleCampaign(flashSaleCampaign))
                .ToArrayAsync();

            return Results.Ok(flashSaleCampaignList);
        }).WithName("GetFlashSaleCampaigns");

        flashSaleCampaigns.MapGet("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var campaign = await dbContext.FlashSaleCampaigns
                .AsNoTracking()
                .Where(campaign => campaign.Id == id)
                .Select(campaign => FlashSaleCampaignResponse.FromFlashSaleCampaign(campaign))
                .FirstOrDefaultAsync();

            return campaign is null ? Results.NotFound() : Results.Ok(campaign);
        })
        .WithName("GetFlashSaleCampaignById");

        flashSaleCampaigns.MapPost("/", async (CreateFlashSaleCampaignRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = FlashSaleCampaignValidator.Validate(request.Name, request.StartsAt, request.EndsAt);

            if (validationError is not null)
            {
                return Results.BadRequest(new {message = validationError});
            }

            var now = DateTimeOffset.UtcNow;
            var campaign = new FlashSaleCampaign
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                IsActive = request.IsActive,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.FlashSaleCampaigns.Add(campaign);
            await dbContext.SaveChangesAsync();

            return Results.CreatedAtRoute(
                "GetFlashSaleCampaignById",
                new { id = campaign.Id },
                FlashSaleCampaignResponse.FromFlashSaleCampaign(campaign));
        }).WithName("CreateFlashSaleCampaign");

        flashSaleCampaigns.MapPut("/{id:guid}", async (Guid id, UpdateFlashSaleCampaignRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = FlashSaleCampaignValidator.Validate(request.Name, request.StartsAt, request.EndsAt);

            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var campaign = await dbContext.FlashSaleCampaigns.FindAsync(id);
            if (campaign is null)
            {
                return Results.NotFound();
            }

            campaign.Name = request.Name.Trim();
            campaign.IsActive = request.IsActive;
            campaign.StartsAt = request.StartsAt;
            campaign.EndsAt = request.EndsAt;
            campaign.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();
            return Results.Ok(FlashSaleCampaignResponse.FromFlashSaleCampaign(campaign));
        }).WithName("UpdateFlashSaleCampaign");

        flashSaleCampaigns.MapDelete("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var campaign = await dbContext.FlashSaleCampaigns.FindAsync(id);

            if (campaign is null)
            {
                return Results.NotFound();
            }

            dbContext.FlashSaleCampaigns.Remove(campaign);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteFlashSaleCampaign");

        return flashSaleCampaigns;
    }
}

public static class FlashSaleCampaignValidator
{
    public static string? Validate(string name, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Campaign name is required.";
        }

        if (endsAt <= startsAt)
        {
            return "Campaign end time must be later than start time.";
        }

        return null;
    }
}

public sealed record UpdateFlashSaleCampaignRequest(
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsActive);

public sealed record CreateFlashSaleCampaignRequest(
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsActive = true);

public sealed record FlashSaleCampaignResponse(
    Guid Id, 
    string Name, 
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static FlashSaleCampaignResponse FromFlashSaleCampaign(FlashSaleCampaign campaign)
    {
        return new FlashSaleCampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.StartsAt,
            campaign.EndsAt,
            campaign.IsActive,
            campaign.CreatedAt,
            campaign.UpdatedAt);
    }
}
