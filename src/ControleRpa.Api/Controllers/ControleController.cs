using ControleRpa.Api.Models;
using ControleRpa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleRpa.Api.Controllers;

[ApiController]
[Route("api/controle")]
[Produces("application/json")]
public class ControleController : ControllerBase
{
    private readonly IControleService _service;
    private readonly ILogger<ControleController> _logger;

    public ControleController(IControleService service, ILogger<ControleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{chaveNota}")]
    [ProducesResponseType(typeof(ControleNotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(string chaveNota, CancellationToken ct)
    {
        var result = await _service.ObterPorChaveAsync(chaveNota, ct);
        return result is null ? NotFound(new { message = $"Nota '{chaveNota}' não encontrada." }) : Ok(result);
    }

    [HttpGet("{chaveNota}/pode-processar")]
    [ProducesResponseType(typeof(PodeProcessarResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PodeProcessar(string chaveNota, CancellationToken ct)
    {
        var result = await _service.PodeProcessarAsync(chaveNota, ct);
        return Ok(result);
    }

    [HttpPost("iniciar")]
    [ProducesResponseType(typeof(ControleNotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Iniciar([FromBody] IniciarNotaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ChaveNota))
            return BadRequest(new { message = "ChaveNota é obrigatória." });

        if (request.IdExecucao == Guid.Empty)
            return BadRequest(new { message = "IdExecucao é obrigatório." });

        try
        {
            var result = await _service.IniciarOuObterAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar nota {ChaveNota}", request.ChaveNota);
            return StatusCode(500, new { message = "Erro interno ao iniciar controle.", detail = ex.Message });
        }
    }

    [HttpPut("{chaveNota}/status")]
    [ProducesResponseType(typeof(ControleNotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatus(string chaveNota, [FromBody] AtualizarStatusRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status é obrigatório." });

        try
        {
            var result = await _service.AtualizarStatusAsync(chaveNota, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Nota '{chaveNota}' não encontrada." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status da nota {ChaveNota}", chaveNota);
            return StatusCode(500, new { message = "Erro interno ao atualizar status.", detail = ex.Message });
        }
    }

    [HttpPost("{chaveNota}/log")]
    [ProducesResponseType(typeof(LogNotaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarLog(
        string chaveNota,
        [FromQuery] Guid idExecucao,
        [FromBody] RegistrarLogRequest request,
        CancellationToken ct)
    {
        if (idExecucao == Guid.Empty)
            return BadRequest(new { message = "idExecucao (query) é obrigatório." });

        if (string.IsNullOrWhiteSpace(request.Passo))
            return BadRequest(new { message = "Passo é obrigatório." });

        try
        {
            var result = await _service.RegistrarLogAsync(chaveNota, idExecucao, request, ct);
            return CreatedAtAction(nameof(ListarLogs), new { chaveNota }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar log da nota {ChaveNota}", chaveNota);
            return StatusCode(500, new { message = "Erro interno ao registrar log.", detail = ex.Message });
        }
    }

    [HttpGet("{chaveNota}/logs")]
    [ProducesResponseType(typeof(IReadOnlyList<LogNotaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarLogs(string chaveNota, CancellationToken ct)
    {
        var result = await _service.ListarLogsAsync(chaveNota, ct);
        return Ok(result);
    }

    [HttpGet("execucao/{idExecucao}")]
    [ProducesResponseType(typeof(IReadOnlyList<ControleNotaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPorExecucao(Guid idExecucao, CancellationToken ct)
    {
        var result = await _service.ListarPorExecucaoAsync(idExecucao, ct);
        return Ok(result);
    }
}
