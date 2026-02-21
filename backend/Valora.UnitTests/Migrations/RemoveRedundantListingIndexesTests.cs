using Xunit;
using Microsoft.EntityFrameworkCore.Migrations;
using Valora.Infrastructure.Migrations;
using System.Reflection;

namespace Valora.UnitTests.Migrations;

public class RemoveRedundantListingIndexesTests
{
    [Fact]
    public void Migration_UpAndDown_ShouldExecuteWithoutError()
    {
        var migration = new RemoveRedundantListingIndexes();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        var upMethod = typeof(RemoveRedundantListingIndexes).GetMethod("Up", BindingFlags.NonPublic | BindingFlags.Instance);
        var downMethod = typeof(RemoveRedundantListingIndexes).GetMethod("Down", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(upMethod);
        Assert.NotNull(downMethod);

        upMethod.Invoke(migration, new object[] { builder });
        downMethod.Invoke(migration, new object[] { builder });

        Assert.NotEmpty(builder.Operations);
    }
}
