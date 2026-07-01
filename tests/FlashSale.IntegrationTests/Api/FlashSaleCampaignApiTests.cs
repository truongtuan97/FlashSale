using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlashSale.IntegrationTests.Api;

public sealed class FlashSaleCampaignApiTests
{
    [Fact]
    public async Task Can_create_update_and_delete_campaign()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/flash-sale-campaigns", new
        {
            name = "Integration Test Campaign",
            startsAt = DateTimeOffset.UtcNow,
            endsAt = DateTimeOffset.UtcNow.AddDays(1),
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdCampaign = await ReadJsonAsync(createResponse);
        var campaignId = createdCampaign.RootElement.GetProperty("id").GetGuid();

        var updatedStartsAt = DateTimeOffset.UtcNow.AddDays(2);
        var updatedEndsAt = updatedStartsAt.AddDays(2);
        var updateResponse = await client.PutAsJsonAsync($"/api/flash-sale-campaigns/{campaignId}", new
        {
            name = "Updated Integration Test Campaign",
            startsAt = updatedStartsAt,
            endsAt = updatedEndsAt,
            isActive = false
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCampaign = await ReadJsonAsync(updateResponse);
        Assert.Equal("Updated Integration Test Campaign", updatedCampaign.RootElement.GetProperty("name").GetString());
        Assert.False(updatedCampaign.RootElement.GetProperty("isActive").GetBoolean());

        var getResponse = await client.GetAsync($"/api/flash-sale-campaigns/{campaignId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/flash-sale-campaigns/{campaignId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedResponse = await client.GetAsync($"/api/flash-sale-campaigns/{campaignId}");

        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
