using ControleRpa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleRpa.Api.Data;

public class RpaDbContext : DbContext
{
    public RpaDbContext(DbContextOptions<RpaDbContext> options) : base(options) { }

    public DbSet<ControleProcessamentoNota> Controles => Set<ControleProcessamentoNota>();
    public DbSet<LogProcessamentoNota> Logs => Set<LogProcessamentoNota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ControleProcessamentoNota>(e =>
        {
            e.ToTable("ControleProcessamentoNota");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChaveNota).IsUnique();
            e.Property(x => x.ChaveNota).HasMaxLength(120).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.PassoAtual).HasMaxLength(80);
            e.Property(x => x.Mensagem).HasMaxLength(2000);
            e.Property(x => x.DadosChave).HasMaxLength(500);
            e.Property(x => x.EvidenciaPath).HasMaxLength(500);
            e.Property(x => x.UsuarioRobo).HasMaxLength(100);
            e.Property(x => x.DataCriacao).HasPrecision(3);
            e.Property(x => x.DataAtualizacao).HasPrecision(3);
        });

        modelBuilder.Entity<LogProcessamentoNota>(e =>
        {
            e.ToTable("LogProcessamentoNota");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ChaveNota, x.DataRegistro });
            e.HasIndex(x => x.IdExecucao);
            e.Property(x => x.ChaveNota).HasMaxLength(120).IsRequired();
            e.Property(x => x.Passo).HasMaxLength(80).IsRequired();
            e.Property(x => x.StatusPasso).HasMaxLength(30).IsRequired();
            e.Property(x => x.TipoExcecao).HasMaxLength(20);
            e.Property(x => x.Mensagem).HasMaxLength(2000);
            e.Property(x => x.EvidenciaPath).HasMaxLength(500);
            e.Property(x => x.DataRegistro).HasPrecision(3);
        });
    }
}
