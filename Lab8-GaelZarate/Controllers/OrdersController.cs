using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab8_GaelZarate.Data;
using Lab8_GaelZarate.Models;

namespace Lab8_GaelZarate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders
            .AsNoTracking()
            .ToListAsync();
    }

    // ✅ EJERCICIO 10: todos los pedidos con sus productos (nombre) y cantidad
    // GET: api/Orders/with-items
    [HttpGet("with-items")]
    public async Task<ActionResult<IEnumerable<object>>> GetAllOrdersWithItems()
    {
        var data = await _context.Orders
            .Include(o => o.Client) // opcional: info del cliente
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Select(o => new
            {
                o.OrderId,
                o.OrderDate,
                Client = new { o.ClientId, o.Client.Name, o.Client.Email },
                Items = o.OrderDetails.Select(od => new
                {
                    od.ProductId,
                    ProductName = od.Product.Name,
                    od.Quantity
                })
            })
            .AsNoTracking()
            .ToListAsync();

        // Si prefieres 404 cuando no hay pedidos, descomenta:
        // if (data.Count == 0) return NotFound("No existen pedidos.");
        return Ok(data);
    }

    // ✅ EJERCICIO 3: productos en una orden (nombre + cantidad)
    // GET: api/Orders/{id}/products
    [HttpGet("{id:int}/products")]
    public async Task<ActionResult<IEnumerable<object>>> GetOrderProducts(int id)
    {
        var exists = await _context.Orders.AnyAsync(o => o.OrderId == id);
        if (!exists) return NotFound($"No existe la orden con id {id}.");

        var items = await _context.OrderDetails
            .Where(od => od.OrderId == id)
            .Select(od => new
            {
                od.ProductId,
                ProductName = od.Product.Name,
                od.Quantity
            })
            .AsNoTracking()
            .ToListAsync();

        if (items.Count == 0)
            return NotFound($"La orden {id} no tiene productos.");

        return Ok(items);
    }

    // ✅ EJERCICIO 4: cantidad total de productos en una orden
    // GET: api/Orders/{id}/total-quantity
    [HttpGet("{id:int}/total-quantity")]
    public async Task<ActionResult<object>> GetOrderTotalQuantity(int id)
    {
        var exists = await _context.Orders.AnyAsync(o => o.OrderId == id);
        if (!exists) return NotFound($"No existe la orden con id {id}.");

        var total = await _context.OrderDetails
            .Where(od => od.OrderId == id)
            .Select(od => (int?)od.Quantity)
            .SumAsync() ?? 0;

        return Ok(new { OrderId = id, TotalQuantity = total });
    }

    // ✅ EJERCICIO 6: pedidos después de una fecha
    // GET: api/Orders/after-date?date=2025-05-01
    [HttpGet("after-date")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersAfterDate([FromQuery] DateTime date)
    {
        if (date == default)
            return BadRequest("Debe proporcionar una fecha válida en el formato YYYY-MM-DD.");

        var pedidos = await _context.Orders
            .Where(o => o.OrderDate > date)
            .Include(o => o.Client)           // opcional
            .AsNoTracking()
            .ToListAsync();

        if (pedidos.Count == 0)
            return NotFound($"No hay pedidos posteriores a {date:yyyy-MM-dd}.");

        return Ok(pedidos);
    }

    // GET: api/Orders/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        return order is null ? NotFound() : order;
    }

    // POST: api/Orders
    [HttpPost]
    public async Task<ActionResult<Order>> PostOrder(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
    }

    // PUT: api/Orders/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutOrder(int id, Order order)
    {
        if (id != order.OrderId) return BadRequest();

        _context.Entry(order).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Orders/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool OrderExists(int id) =>
        _context.Orders.Any(e => e.OrderId == id);
}
