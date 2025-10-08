using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab8_GaelZarate.Data;
using Lab8_GaelZarate.Models;

namespace Lab8_GaelZarate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Clients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetClients()
    {
        return await _context.Clients
            .AsNoTracking()
            .ToListAsync();
    }

    // EJERCICIO 1: filtrar por nombre (prefijo, case-insensitive en PostgreSQL)
    // GET: api/Clients/search?name=Juan
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Client>>> SearchClients([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Debe especificar un nombre para filtrar.");

        var clientes = await _context.Clients
            .Where(c => EF.Functions.ILike(c.Name, $"{name}%"))
            .AsNoTracking()
            .ToListAsync();

        if (clientes.Count == 0)
            return NotFound("No se encontraron clientes con ese nombre.");

        return Ok(clientes);
    }

    // EJERCICIO 9: cliente con mayor número de pedidos
    // GET: api/Clients/most-orders
    [HttpGet("most-orders")]
    public async Task<ActionResult<object>> GetClientWithMostOrders()
    {
        var result = await _context.Clients
            .Select(c => new
            {
                c.ClientId,
                c.Name,
                c.Email,
                OrdersCount = c.Orders.Count()
            })
            .OrderByDescending(x => x.OrdersCount)
            .ThenBy(x => x.ClientId)
            .FirstOrDefaultAsync();

        if (result is null) return NotFound("No hay clientes registrados.");
        if (result.OrdersCount == 0) return NotFound("No hay pedidos registrados.");

        return Ok(result);
    }

    // ✅ EJERCICIO 11: productos vendidos a un cliente específico (solo nombres, únicos)
    // GET: api/Clients/{id}/products-sold
    [HttpGet("{id:int}/products-sold")]
    public async Task<ActionResult<IEnumerable<string>>> GetProductsSoldToClient(int id)
    {
        // Verificar existencia del cliente
        var exists = await _context.Clients.AnyAsync(c => c.ClientId == id);
        if (!exists) return NotFound($"No existe el cliente con id {id}.");

        // Pedidos del cliente -> Detalles -> Nombres de producto (DISTINCT)
        var productNames = await _context.OrderDetails
            .Where(od => od.Order.ClientId == id)
            .Select(od => od.Product.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        if (productNames.Count == 0)
            return NotFound($"El cliente {id} no tiene productos vendidos.");

        return Ok(productNames);
    }

    // (Opcional extra) Productos vendidos a un cliente con cantidad total por producto
    // GET: api/Clients/{id}/products-sold-with-qty
    [HttpGet("{id:int}/products-sold-with-qty")]
    public async Task<ActionResult<IEnumerable<object>>> GetProductsSoldToClientWithQty(int id)
    {
        var exists = await _context.Clients.AnyAsync(c => c.ClientId == id);
        if (!exists) return NotFound($"No existe el cliente con id {id}.");

        var items = await _context.OrderDetails
            .Where(od => od.Order.ClientId == id)
            .GroupBy(od => new { od.ProductId, od.Product.Name })
            .Select(g => new
            {
                g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.ProductName)
            .AsNoTracking()
            .ToListAsync();

        if (items.Count == 0)
            return NotFound($"El cliente {id} no tiene productos vendidos.");

        return Ok(items);
    }

    // GET: api/Clients/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        return client is null ? NotFound() : client;
    }

    // PUT: api/Clients/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutClient(int id, Client client)
    {
        if (id != client.ClientId) return BadRequest();

        _context.Entry(client).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // POST: api/Clients
    [HttpPost]
    public async Task<ActionResult<Client>> PostClient(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClient), new { id = client.ClientId }, client);
    }

    // DELETE: api/Clients/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client is null) return NotFound();

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ClientExists(int id) =>
        _context.Clients.Any(e => e.ClientId == id);
}
