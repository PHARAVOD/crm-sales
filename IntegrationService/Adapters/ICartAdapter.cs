using IntegrationService.Models;

namespace IntegrationService.Adapters;

public interface ICartAdapter
{
    Task<CartDto> GetCartAsync();
    Task<CartItemDto> AddToCartAsync(CartItemDto item);
    Task ClearCartAsync();
}