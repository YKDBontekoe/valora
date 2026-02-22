using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace Valora.UnitTests.Infrastructure.Persistence.Configurations;

public class BatchJobConfigurationTests
{
    [Fact]
    public void BatchJob_ShouldHaveCheckConstraint_ForType_IncludingAllCitiesIngestion()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "BatchJobConfigurationTestDb")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        // Get the design-time model which contains check constraints
        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(BatchJob));

        // Assert
        entityType.Should().NotBeNull();
        var constraints = entityType!.GetCheckConstraints();
        var typeConstraint = constraints.FirstOrDefault(c => c.Name == "CK_BatchJob_Type");

        typeConstraint.Should().NotBeNull("BatchJob entity should have a check constraint named 'CK_BatchJob_Type'");
        // The SQL string contains single quotes escaped or not depending on context, check for the string content
        typeConstraint!.Sql.Should().Contain("'AllCitiesIngestion'");
    }
}
