/*
==============================================================================================
  Tabelas para controle de idempotência e observabilidade do Desafio Cadmus (SQL Server 2019+)
==============================================================================================
*/

USE master;
GO

-- Ajuste o nome do banco conforme o ambiente
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'RpaControle')
BEGIN
    CREATE DATABASE RpaControle;
END
GO

USE RpaControle;
GO

IF OBJECT_ID(N'dbo.ControleProcessamentoNota', N'U') IS NOT NULL
    DROP TABLE dbo.ControleProcessamentoNota;
GO

CREATE TABLE dbo.ControleProcessamentoNota
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    ChaveNota           NVARCHAR(120)        NOT NULL,   
    IdExecucao          UNIQUEIDENTIFIER     NOT NULL,   
    Status              NVARCHAR(30)         NOT NULL,   
    Tentativas          INT                  NOT NULL CONSTRAINT DF_Controle_Tentativas DEFAULT (0),
    PassoAtual          NVARCHAR(80)         NULL,       
    Mensagem            NVARCHAR(2000)       NULL,       
    DadosChave          NVARCHAR(500)        NULL,       
    EvidenciaPath       NVARCHAR(500)        NULL,       
    UsuarioRobo         NVARCHAR(100)        NULL,
    DataCriacao         DATETIME2(3)         NOT NULL CONSTRAINT DF_Controle_DataCriacao DEFAULT (SYSUTCDATETIME()),
    DataAtualizacao     DATETIME2(3)         NOT NULL CONSTRAINT DF_Controle_DataAtualizacao DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ControleProcessamentoNota PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ControleProcessamentoNota_ChaveNota UNIQUE (ChaveNota),
    CONSTRAINT CK_Controle_Status CHECK (Status IN (
        N'PENDENTE',
        N'EM_PROCESSAMENTO',
        N'SUCESSO',
        N'REJEITADA_NEGOCIO',
        N'FALHA_TECNICA',
        N'JA_EXISTENTE'
    ))
);
GO

CREATE NONCLUSTERED INDEX IX_Controle_Status_DataAtualizacao
    ON dbo.ControleProcessamentoNota (Status, DataAtualizacao DESC);
GO

CREATE NONCLUSTERED INDEX IX_Controle_IdExecucao
    ON dbo.ControleProcessamentoNota (IdExecucao);
GO

IF OBJECT_ID(N'dbo.LogProcessamentoNota', N'U') IS NOT NULL
    DROP TABLE dbo.LogProcessamentoNota;
GO

CREATE TABLE dbo.LogProcessamentoNota
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    ChaveNota           NVARCHAR(120)        NOT NULL,
    IdExecucao          UNIQUEIDENTIFIER     NOT NULL,
    Passo               NVARCHAR(80)         NOT NULL,
    StatusPasso         NVARCHAR(30)         NOT NULL,   
    TipoExcecao         NVARCHAR(20)         NULL,       
    Mensagem            NVARCHAR(2000)       NULL,
    DetalheTecnico      NVARCHAR(MAX)        NULL,       
    DadosJson           NVARCHAR(MAX)        NULL,       
    EvidenciaPath       NVARCHAR(500)        NULL,
    Tentativa           INT                  NULL,
    DataRegistro        DATETIME2(3)         NOT NULL CONSTRAINT DF_Log_DataRegistro DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_LogProcessamentoNota PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_Log_ChaveNota_Data
    ON dbo.LogProcessamentoNota (ChaveNota, DataRegistro DESC);
GO

CREATE NONCLUSTERED INDEX IX_Log_IdExecucao
    ON dbo.LogProcessamentoNota (IdExecucao);
GO

CREATE OR ALTER VIEW dbo.vw_ResumoUltimaExecucao
AS
SELECT
    c.ChaveNota,
    c.Status,
    c.Tentativas,
    c.PassoAtual,
    c.Mensagem,
    c.DadosChave,
    c.EvidenciaPath,
    c.IdExecucao,
    c.DataAtualizacao,
    (SELECT COUNT(*) FROM dbo.LogProcessamentoNota l WHERE l.ChaveNota = c.ChaveNota) AS QtdLogs
FROM dbo.ControleProcessamentoNota c;
GO

PRINT 'Tabelas e view criadas com sucesso.';
GO
