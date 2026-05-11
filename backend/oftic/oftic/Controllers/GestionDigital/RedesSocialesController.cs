using Comun.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Negocio.Interfaz;

namespace Api.Controllers;

[ApiController]
[Route("api/gestion-digital/redes-sociales")]
[Route("api/[controller]")]
[Authorize]
public class RedesSocialesController : ControllerBase
{
    private readonly IDbRedesSocialesService _service;

    public RedesSocialesController(IDbRedesSocialesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var redesSociales = await _service.GetAllAsync();
        return Ok(redesSociales);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var redesSociales = await _service.GetByIdAsync(id);
        if (redesSociales == null)
        {
            return NotFound();
        }
        return Ok(redesSociales);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DtoRedesSociales redesSociales)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (usuario, _) = ObtenerAuditoria();

        var result = await _service.CreateAsync(redesSociales, usuario);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DtoRedesSociales redesSociales)
    {
        if (!ModelState.IsValid || id != redesSociales.Id)
        {
            return BadRequest(ModelState);
        }

        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var (usuario, _) = ObtenerAuditoria();

        var result = await _service.UpdateAsync(redesSociales, usuario);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var (usuario, _) = ObtenerAuditoria();

        var result = await _service.DeleteAsync(id, usuario);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return NoContent();
    }

    [HttpGet("dominios")]
    public async Task<IActionResult> GetDominios()
    {
        var dominios = await _service.GetDominiosAsync();
        return Ok(dominios);
    }

    [HttpGet("unidades")]
    public async Task<IActionResult> GetUnidades()
    {
        var unidades = await _service.GetUnidadesAsync();
        return Ok(unidades);
    }

    private (string usuario, string maquina) ObtenerAuditoria()
    {
        var rawId = User.FindFirstValue("id_usuario")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid")
            ?? User.Identity?.Name;

        var usuario = long.TryParse(rawId, out var usuarioId) && usuarioId > 0
            ? usuarioId.ToString()
            : "1";

        var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? Environment.MachineName
            ?? "N/A";

        return (usuario, maquina);
    }
}
