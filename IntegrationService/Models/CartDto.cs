namespace IntegrationService.Models;

public class CartItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; }
    public decimal Total => Items?.Sum(x => x.Price * x.Quantity) ?? 0;
}