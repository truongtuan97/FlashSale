using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlashSale.IntegrationTests.Api;

internal sealed class FlashSaleApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"FlashSaleTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<FlashSaleDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FlashSaleDbContext>>();

            services.AddDbContext<FlashSaleDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
