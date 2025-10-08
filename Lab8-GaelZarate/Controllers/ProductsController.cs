using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab8_GaelZarate.Data;
using Lab8_GaelZarate.Models;

namespace Lab8_GaelZarate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    // EJERCICIO 2: productos con precio > minPrice
    // GET: api/Products/price-greater-than?minPrice=20
    [HttpGet("price-greater-than")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsPriceGreaterThan([FromQuery] decimal minPrice = 20m)
    {
        var items = await _context.Products
            .Where(p => p.Price > minPrice)
            .AsNoTracking()
            .ToListAsync();

        if (items.Count == 0)
            return NotFound($"No hay productos con precio mayor a {minPrice}.");

        return Ok(items);
    }

    // EJERCICIO 5: producto más caro
    // GET: api/Products/most-expensive
    [HttpGet("most-expensive")]
    public async Task<ActionResult<Product>> GetMostExpensiveProduct()
    {
        var product = await _context.Products
            .OrderByDescending(p => p.Price)
            .FirstOrDefaultAsync();

        if (product is null)
            return NotFound("No hay productos registrados.");

        return Ok(product);
    }

    // EJERCICIO 7: precio promedio de los productos
    // GET: api/Products/average-price
    [HttpGet("average-price")]
    public async Task<ActionResult<object>> GetAveragePrice()
    {
        var avg = await _context.Products
            .Select(p => (decimal?)p.Price)
            .AverageAsync() ?? 0m;

        var count = await _context.Products.CountAsync();

        return Ok(new
        {
            ProductsCount = count,
            AveragePrice = avg
        });
    }

    // EJERCICIO 8: productos sin descripción
    // GET: api/Products/without-description
    [HttpGet("without-description")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsWithoutDescription()
    {
        var items = await _context.Products
            .Where(p => string.IsNullOrEmpty(p.Description))
            .AsNoTracking()
            .ToListAsync();

        if (items.Count == 0)
            return NotFound("Todos los productos tienen descripción.");

        return Ok(items);
    }

    // ✅ EJERCICIO 12: clientes que han comprado un producto específico (por ProductId)
    // GET: api/Products/{id}/buyers
    [HttpGet("{id:int}/buyers")]
    public async Task<ActionResult<IEnumerable<object>>> GetBuyersForProduct(int id)
    {
        // Verifica que el producto exista
        var productExists = await _context.Products.AnyAsync(p => p.ProductId == id);
        if (!productExists) return NotFound($"No existe el producto con id {id}.");

        // OrderDetails -> Orders -> Clients (DISTINCT)
        var buyers = await _context.OrderDetails
            .Where(od => od.ProductId == id)
            .Select(od => new
            {
                od.Order.Client.ClientId,
                od.Order.Client.Name,
                od.Order.Client.Email
            })
            .Distinct()              // evita clientes repetidos si compraron varias veces
            .OrderBy(c => c.ClientId)
            .AsNoTracking()
            .ToListAsync();

        if (buyers.Count == 0)
            return NotFound($"El producto {id} no registra compradores.");

        return Ok(buyers);
    }

    // (Opcional) mismos compradores pero con cantidad total comprada por cliente
    // GET: api/Products/{id}/buyers-with-qty
    [HttpGet("{id:int}/buyers-with-qty")]
    public async Task<ActionResult<IEnumerable<object>>> GetBuyersForProductWithQty(int id)
    {
        var productExists = await _context.Products.AnyAsync(p => p.ProductId == id);
        if (!productExists) return NotFound($"No existe el producto con id {id}.");

        var buyers = await _context.OrderDetails
            .Where(od => od.ProductId == id)
            .GroupBy(od => new { od.Order.ClientId, od.Order.Client.Name, od.Order.Client.Email })
            .Select(g => new
            {
                g.Key.ClientId,
                g.Key.Name,
                g.Key.Email,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.ClientId)
            .AsNoTracking()
            .ToListAsync();

        if (buyers.Count == 0)
            return NotFound($"El producto {id} no registra compradores.");

        return Ok(buyers);
    }

    // GET: api/Products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product is null ? NotFound() : product;
    }

    // PUT: api/Products/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProduct(int id, Product product)
    {
        if (id != product.ProductId) return BadRequest();

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // POST: api/Products
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    // DELETE: api/Products/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id) =>
        _context.Products.Any(e => e.ProductId == id);
}
