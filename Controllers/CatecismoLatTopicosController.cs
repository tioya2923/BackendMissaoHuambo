using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatecismoLatTopicosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatecismoLatTopicosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatecismoLatTopico>>> GetAll()
        => await _context.CatecismoLatTopicos.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<CatecismoLatTopico>> GetById(int id)
    {
        var item = await _context.CatecismoLatTopicos.FirstOrDefaultAsync(t => t.Id == id);
        if (item == null) return NotFound();
        return item;
    }
}
