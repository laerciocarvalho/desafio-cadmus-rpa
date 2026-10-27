using ControleRpa.Api.Data;
using ControleRpa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleRpa.Api.Services;

public interface IControleService
{
    Task<ControleNotaResponse?> ObterPorChaveAsync(string chaveNota, CancellationToken ct = default);
    Task<PodeProcessarResponse> PodeProcessarAsync(string chaveNota, CancellationToken ct = default);
    Task<ControleNotaResponse> IniciarOuObterAsync(IniciarNotaRequest request, CancellationToken ct = default);
    Task<ControleNotaResponse> AtualizarStatusAsync(string chaveNota, AtualizarStatusRequest request, CancellationToken ct = default);
    Task<LogNotaResponse> RegistrarLogAsync(string chaveNota, Guid idExecucao, RegistrarLogRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LogNotaResponse>> ListarLogsAsync(string chaveNota, CancellationToken ct = default);
    Task<IReadOnlyList<ControleNotaResponse>> ListarPorExecucaoAsync(Guid idExecucao, CancellationToken ct = default);
}

public class ControleService : IControleService
{
    private readonly RpaDbContext _db;

    private static readonly HashSet<string> StatusFinais = new(StringComparer.OrdinalIgnoreCase)
    {
        StatusNota.Sucesso,
        StatusNota.RejeitadaNegocio,
        StatusNota.JaExistente
    };

    public ControleService(RpaDbContext db) => _db = db;

    public async Task<ControleNotaResponse?> ObterPorChaveAsync(string chaveNota, CancellationToken ct = default)
    {
        var entity = await _db.Controles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChaveNota == chaveNota, ct);

        return entity is null ? null : Map(entity);
    }

    public async Task<PodeProcessarResponse> PodeProcessarAsync(string chaveNota, CancellationToken ct = default)
    {
        var entity = await _db.Controles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChaveNota == chaveNota, ct);

        if (entity is null)
        {
            return new PodeProcessarResponse(
                PodeProcessar: true,
                StatusAtual: "NAO_EXISTE",
                Motivo: "Nota ainda não possui registro de controle. Pode iniciar.",
                Controle: null);
        }

        if (StatusFinais.Contains(entity.Status))
        {
            return new PodeProcessarResponse(
                PodeProcessar: false,
                StatusAtual: entity.Status,
                Motivo: $"Nota já finalizada com status '{entity.Status}'. Não reprocessar.",
                Controle: Map(entity));
        }
        
        return new PodeProcessarResponse(
            PodeProcessar: true,
            StatusAtual: entity.Status,
            Motivo: entity.Status == StatusNota.EmProcessamento
                ? "Nota estava EM_PROCESSAMENTO. Pode retomar (verificar se já existe no ERP antes de lançar)."
                : $"Status atual '{entity.Status}'. Pode processar/retentar.",
            Controle: Map(entity));
    }

    public async Task<ControleNotaResponse> IniciarOuObterAsync(IniciarNotaRequest request, CancellationToken ct = default)
    {
        var existente = await _db.Controles
            .FirstOrDefaultAsync(x => x.ChaveNota == request.ChaveNota, ct);

        if (existente is not null)
        {
            if (StatusFinais.Contains(existente.Status))
                return Map(existente);
            
            existente.IdExecucao = request.IdExecucao;
            existente.Status = StatusNota.EmProcessamento;
            existente.UsuarioRobo = request.UsuarioRobo ?? existente.UsuarioRobo;
            existente.DadosChave = request.DadosChave ?? existente.DadosChave;
            existente.DataAtualizacao = DateTime.UtcNow;
            
            await _db.SaveChangesAsync(ct);
            return Map(existente);
        }

        var novo = new ControleProcessamentoNota
        {
            ChaveNota = request.ChaveNota,
            IdExecucao = request.IdExecucao,
            Status = StatusNota.EmProcessamento,
            Tentativas = 0,
            UsuarioRobo = request.UsuarioRobo,
            DadosChave = request.DadosChave,
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow
        };

        _db.Controles.Add(novo);
        await _db.SaveChangesAsync(ct);
        return Map(novo);
    }

    public async Task<ControleNotaResponse> AtualizarStatusAsync(string chaveNota, AtualizarStatusRequest request, CancellationToken ct = default)
    {
        if (!StatusNota.Todos.Contains(request.Status))
            throw new ArgumentException($"Status inválido: {request.Status}. Valores permitidos: {string.Join(", ", StatusNota.Todos)}");

        var entity = await _db.Controles
            .FirstOrDefaultAsync(x => x.ChaveNota == chaveNota, ct)
            ?? throw new KeyNotFoundException($"Nota '{chaveNota}' não encontrada no controle.");

        entity.Status = request.Status;
        entity.PassoAtual = request.PassoAtual ?? entity.PassoAtual;
        entity.Mensagem = request.Mensagem ?? entity.Mensagem;
        entity.DadosChave = request.DadosChave ?? entity.DadosChave;
        entity.EvidenciaPath = request.EvidenciaPath ?? entity.EvidenciaPath;

        if (request.Tentativas.HasValue)
            entity.Tentativas = request.Tentativas.Value;

        entity.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<LogNotaResponse> RegistrarLogAsync(string chaveNota, Guid idExecucao, RegistrarLogRequest request, CancellationToken ct = default)
    {
        var log = new LogProcessamentoNota
        {
            ChaveNota = chaveNota,
            IdExecucao = idExecucao,
            Passo = request.Passo,
            StatusPasso = request.StatusPasso,
            TipoExcecao = request.TipoExcecao,
            Mensagem = request.Mensagem,
            DetalheTecnico = request.DetalheTecnico,
            DadosJson = request.DadosJson,
            EvidenciaPath = request.EvidenciaPath,
            Tentativa = request.Tentativa,
            DataRegistro = DateTime.UtcNow
        };

        _db.Logs.Add(log);
        await _db.SaveChangesAsync(ct);
        return MapLog(log);
    }

    public async Task<IReadOnlyList<LogNotaResponse>> ListarLogsAsync(string chaveNota, CancellationToken ct = default)
    {
        var logs = await _db.Logs
            .AsNoTracking()
            .Where(x => x.ChaveNota == chaveNota)
            .OrderByDescending(x => x.DataRegistro)
            .Take(100)
            .ToListAsync(ct);

        return logs.Select(MapLog).ToList();
    }

    public async Task<IReadOnlyList<ControleNotaResponse>> ListarPorExecucaoAsync(Guid idExecucao, CancellationToken ct = default)
    {
        var itens = await _db.Controles
            .AsNoTracking()
            .Where(x => x.IdExecucao == idExecucao)
            .OrderBy(x => x.ChaveNota)
            .ToListAsync(ct);

        return itens.Select(Map).ToList();
    }

    private static ControleNotaResponse Map(ControleProcessamentoNota e) => new(
        e.Id, e.ChaveNota, e.IdExecucao, e.Status, e.Tentativas,
        e.PassoAtual, e.Mensagem, e.DadosChave, e.EvidenciaPath,
        e.UsuarioRobo, e.DataCriacao, e.DataAtualizacao);

    private static LogNotaResponse MapLog(LogProcessamentoNota e) => new(
        e.Id, e.ChaveNota, e.IdExecucao, e.Passo, e.StatusPasso,
        e.TipoExcecao, e.Mensagem, e.DetalheTecnico, e.DadosJson,
        e.EvidenciaPath, e.Tentativa, e.DataRegistro);
}
