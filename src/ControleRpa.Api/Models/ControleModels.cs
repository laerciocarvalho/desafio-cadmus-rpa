namespace ControleRpa.Api.Models;

public static class StatusNota
{
    public const string Pendente         = "PENDENTE";
    public const string EmProcessamento  = "EM_PROCESSAMENTO";
    public const string Sucesso          = "SUCESSO";
    public const string RejeitadaNegocio = "REJEITADA_NEGOCIO";
    public const string FalhaTecnica     = "FALHA_TECNICA";
    public const string JaExistente      = "JA_EXISTENTE";

    public static readonly HashSet<string> Todos = new(StringComparer.OrdinalIgnoreCase)
    {
        Pendente, EmProcessamento, Sucesso, RejeitadaNegocio, FalhaTecnica, JaExistente
    };
}

public static class TipoExcecao
{
    public const string Negocio = "NEGOCIO";
    public const string Tecnica = "TECNICA";
}

public static class StatusPasso
{
    public const string Sucesso = "SUCESSO";
    public const string Erro    = "ERRO";
    public const string Info    = "INFO";
    public const string Retry   = "RETRY";
}

public class ControleProcessamentoNota
{
    public long Id { get; set; }
    public string ChaveNota { get; set; } = null!;
    public Guid IdExecucao { get; set; }
    public string Status { get; set; } = StatusNota.Pendente;
    public int Tentativas { get; set; }
    public string? PassoAtual { get; set; }
    public string? Mensagem { get; set; }
    public string? DadosChave { get; set; }
    public string? EvidenciaPath { get; set; }
    public string? UsuarioRobo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class LogProcessamentoNota
{
    public long Id { get; set; }
    public string ChaveNota { get; set; } = null!;
    public Guid IdExecucao { get; set; }
    public string Passo { get; set; } = null!;
    public string StatusPasso { get; set; } = null!;
    public string? TipoExcecao { get; set; }
    public string? Mensagem { get; set; }
    public string? DetalheTecnico { get; set; }
    public string? DadosJson { get; set; }
    public string? EvidenciaPath { get; set; }
    public int? Tentativa { get; set; }
    public DateTime DataRegistro { get; set; }
}

public record IniciarNotaRequest(
    string ChaveNota,
    Guid IdExecucao,
    string? UsuarioRobo = null,
    string? DadosChave = null
);

public record AtualizarStatusRequest(
    string Status,
    string? PassoAtual = null,
    string? Mensagem = null,
    string? DadosChave = null,
    string? EvidenciaPath = null,
    int? Tentativas = null
);

public record RegistrarLogRequest(
    string Passo,
    string StatusPasso,
    string? TipoExcecao = null,
    string? Mensagem = null,
    string? DetalheTecnico = null,
    string? DadosJson = null,
    string? EvidenciaPath = null,
    int? Tentativa = null
);

public record ControleNotaResponse(
    long Id,
    string ChaveNota,
    Guid IdExecucao,
    string Status,
    int Tentativas,
    string? PassoAtual,
    string? Mensagem,
    string? DadosChave,
    string? EvidenciaPath,
    string? UsuarioRobo,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record LogNotaResponse(
    long Id,
    string ChaveNota,
    Guid IdExecucao,
    string Passo,
    string StatusPasso,
    string? TipoExcecao,
    string? Mensagem,
    string? DetalheTecnico,
    string? DadosJson,
    string? EvidenciaPath,
    int? Tentativa,
    DateTime DataRegistro
);

public record PodeProcessarResponse(
    bool PodeProcessar,
    string StatusAtual,
    string Motivo,
    ControleNotaResponse? Controle
);
