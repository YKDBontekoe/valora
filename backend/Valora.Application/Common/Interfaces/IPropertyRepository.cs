using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IPropertyRepository
{
    Task<Property?> GetPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<Property?> GetPropertyByBagIdAsync(string bagId, CancellationToken ct = default);
    Task<Property> AddPropertyAsync(Property property, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
