using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TipMolde.Application.Dtos.RelatorioDto;
using TipMolde.Application.DTOs.RelatorioDto.Linhas;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Relatorios;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa queries especializadas do modulo de relatorios.
    /// </summary>
    /// <remarks>
    /// Porque: os relatorios precisam de agregacoes transversais a comercio, desenho e producao.
    /// Devolver entidades EF diretamente aqui tornaria o formato dos documentos demasiado acoplado
    /// ao modelo de persistencia e aumentaria o risco de regressao.
    /// </remarks>
    public class RelatorioRepository : IRelatorioRepository
    {
        private static readonly Expression<Func<EncomendaMolde, FichaRelatorioBaseDto>> FltRelatorioBaseProjection = em => new()
        {
            FichaId = em.EncomendaMolde_id,
            Tipo = TipoFicha.FLT,
            MoldeNumero = em.Molde != null ? em.Molde.Numero : string.Empty,
            MoldeNome = em.Molde != null ? em.Molde.Nome : null,
            NumeroMoldeCliente = em.Molde != null ? em.Molde.NumeroMoldeCliente : null,
            ImagemCapaPath = em.Molde != null ? em.Molde.ImagemCapaPath : null,
            NumeroCavidades = em.Molde != null ? em.Molde.Numero_cavidades : 0,
            TipoPedido = em.Molde != null ? em.Molde.TipoPedido : TipoPedido.NOVO_MOLDE,
            ClienteNome = em.Encomenda != null && em.Encomenda.Cliente != null ? em.Encomenda.Cliente.Nome : string.Empty,
            NomeServicoCliente = em.Encomenda != null ? em.Encomenda.NomeServicoCliente : null,
            NumeroProjetoCliente = em.Encomenda != null ? em.Encomenda.NumeroProjetoCliente : null,
            NomeResponsavelCliente = em.Encomenda != null ? em.Encomenda.NomeResponsavelCliente : null,
            DataEntregaPrevista = em.DataEntregaPrevista,
            MaterialInjecao = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.MaterialInjecao : null,
            Contracao = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.Contracao : null,
            TipoInjecao = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.TipoInjecao : null,
            AcabamentoPeca = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.AcabamentoPeca : null,
            MaterialMacho = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.MaterialMacho : null,
            MaterialCavidade = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.MaterialCavidade : null,
            MaterialMovimentos = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.MaterialMovimentos : null,
            SistemaInjecao = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.SistemaInjecao : null,
            Cor = em.Molde != null && em.Molde.Especificacoes != null ? em.Molde.Especificacoes.Cor : null,
            LadoFixo = em.Molde != null && em.Molde.Especificacoes != null && em.Molde.Especificacoes.LadoFixo,
            LadoMovel = em.Molde != null && em.Molde.Especificacoes != null && em.Molde.Especificacoes.LadoMovel
        };

        private static readonly Expression<Func<FichaProducao, FichaRelatorioBaseDto>> FichaRelatorioBaseProjection = f => new()
        {
            FichaId = f.FichaProducao_id,
            Tipo = f.Tipo,
            MoldeNumero = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.Numero : string.Empty,
            MoldeNome = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.Nome : null,
            NumeroMoldeCliente = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.NumeroMoldeCliente : null,
            ImagemCapaPath = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.ImagemCapaPath : null,
            NumeroCavidades = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.Numero_cavidades : 0,
            TipoPedido = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null ? f.EncomendaMolde.Molde.TipoPedido : TipoPedido.NOVO_MOLDE,
            ClienteNome = f.EncomendaMolde != null && f.EncomendaMolde.Encomenda != null && f.EncomendaMolde.Encomenda.Cliente != null ? f.EncomendaMolde.Encomenda.Cliente.Nome : string.Empty,
            NomeServicoCliente = f.EncomendaMolde != null && f.EncomendaMolde.Encomenda != null ? f.EncomendaMolde.Encomenda.NomeServicoCliente : null,
            NumeroProjetoCliente = f.EncomendaMolde != null && f.EncomendaMolde.Encomenda != null ? f.EncomendaMolde.Encomenda.NumeroProjetoCliente : null,
            NomeResponsavelCliente = f.EncomendaMolde != null && f.EncomendaMolde.Encomenda != null ? f.EncomendaMolde.Encomenda.NomeResponsavelCliente : null,
            DataEntregaPrevista = f.EncomendaMolde != null ? f.EncomendaMolde.DataEntregaPrevista : DateTime.UtcNow,
            MaterialInjecao = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.MaterialInjecao : null,
            Contracao = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.Contracao : null,
            TipoInjecao = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.TipoInjecao : null,
            AcabamentoPeca = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.AcabamentoPeca : null,
            MaterialMacho = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.MaterialMacho : null,
            MaterialCavidade = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.MaterialCavidade : null,
            MaterialMovimentos = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.MaterialMovimentos : null,
            SistemaInjecao = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.SistemaInjecao : null,
            Cor = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null ? f.EncomendaMolde.Molde.Especificacoes.Cor : null,
            LadoFixo = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null && f.EncomendaMolde.Molde.Especificacoes.LadoFixo,
            LadoMovel = f.EncomendaMolde != null && f.EncomendaMolde.Molde != null && f.EncomendaMolde.Molde.Especificacoes != null && f.EncomendaMolde.Molde.Especificacoes.LadoMovel
        };

        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor de RelatorioRepository.
        /// </summary>
        /// <param name="context">DbContext usado nas queries especializadas de relatorio.</param>
        public RelatorioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MoldeCicloVidaRelatorioDto?> ObterMoldeCicloVidaAsync(int moldeId)
        {
            var molde = await _context.Moldes
                .AsNoTracking()
                .Include(m => m.EncomendasMoldes)
                    .ThenInclude(em => em.Encomenda)
                        .ThenInclude(e => e!.Cliente)
                .FirstOrDefaultAsync(m => m.Molde_id == moldeId);

            if (molde is null)
                return null;

            var encomendaMoldeAtual = molde.EncomendasMoldes
                .OrderByDescending(em => em.DataEntregaPrevista)
                .FirstOrDefault();

            var pecas = await _context.Pecas
                .AsNoTracking()
                .Where(p => p.Molde_id == moldeId)
                .ToListAsync();

            var pecaIds = pecas.Select(p => p.Peca_id).ToArray();

            var projetos = await _context.Projetos
                .AsNoTracking()
                .Where(p => p.Molde_id == moldeId)
                .Select(p => new MoldeProjetoResumoDto
                {
                    ProjetoId = p.Projeto_id,
                    NomeProjeto = p.NomeProjeto,
                    SoftwareUtilizado = p.SoftwareUtilizado,
                    TipoProjeto = p.TipoProjeto.ToString()
                })
                .ToListAsync();

            var projetoIds = projetos.Select(p => p.ProjetoId).ToArray();

            var totalRevisoes = projetoIds.Length == 0
                ? 0
                : await _context.Revisoes
                    .AsNoTracking()
                    .CountAsync(r => projetoIds.Contains(r.Projeto_id));

            var ultimaRevisaoEm = projetoIds.Length == 0
                ? null
                : await _context.Revisoes
                    .AsNoTracking()
                    .Where(r => projetoIds.Contains(r.Projeto_id))
                    .MaxAsync(r => (DateTime?)(r.DataResposta ?? r.DataEnvioCliente));

            var registos = pecaIds.Length == 0
                ? new List<RegistosProducao>()
                : await _context.RegistosProducao
                    .AsNoTracking()
                    .Where(r => pecaIds.Contains(r.Peca_id))
                    .ToListAsync();

            var fases = await _context.Fases_Producao
                .AsNoTracking()
                .ToDictionaryAsync(f => f.Fases_producao_id, f => f.Nome);

            var ultimosPorPeca = registos
                .GroupBy(r => r.Peca_id)
                .Select(g => g.OrderByDescending(x => x.Data_hora).First())
                .ToList();
            var ultimosPorPecaMap = ultimosPorPeca.ToDictionary(r => r.Peca_id, r => r);

            var maquinacao = 0;
            var erosao = 0;
            var montagem = 0;
            var emEspera = 0;
            var emTrabalho = 0;
            var concluidas = 0;

            foreach (var peca in pecas)
            {
                if (!peca.MaterialRecebido)
                    continue;

                if (!ultimosPorPecaMap.TryGetValue(peca.Peca_id, out var ultimo))
                {
                    emEspera++;
                    continue;
                }

                var nomeFaseAtual = fases.TryGetValue(ultimo.Fase_id, out var faseAtual)
                    ? faseAtual
                    : (NomeFases?)null;

                var estadoAtual = ultimo.Estado_producao;
                var estaAtivaAgora = estadoAtual is EstadoProducao.PREPARACAO or EstadoProducao.EM_CURSO;

                if (estaAtivaAgora)
                {
                    emTrabalho++;

                    switch (nomeFaseAtual)
                    {
                        case NomeFases.MAQUINACAO:
                            maquinacao++;
                            break;
                        case NomeFases.EROSAO:
                            erosao++;
                            break;
                        case NomeFases.MONTAGEM:
                            montagem++;
                            break;
                        default:
                            emEspera++;
                            break;
                    }

                    continue;
                }

                if (nomeFaseAtual == NomeFases.MONTAGEM && estadoAtual == EstadoProducao.CONCLUIDO)
                {
                    montagem++;
                    concluidas++;
                    continue;
                }

                emEspera++;
            }

            var dto = new MoldeCicloVidaRelatorioDto
            {
                MoldeId = molde.Molde_id,
                NumeroMolde = molde.Numero,
                NumeroMoldeCliente = molde.NumeroMoldeCliente,
                NomeMolde = molde.Nome,
                DescricaoMolde = molde.Descricao,
                NumeroCavidades = molde.Numero_cavidades,
                TipoPedido = molde.TipoPedido,
                ClienteNome = encomendaMoldeAtual?.Encomenda?.Cliente?.Nome,
                NumeroEncomendaCliente = encomendaMoldeAtual?.Encomenda?.NumeroEncomendaCliente,
                NumeroProjetoCliente = encomendaMoldeAtual?.Encomenda?.NumeroProjetoCliente,
                NomeResponsavelCliente = encomendaMoldeAtual?.Encomenda?.NomeResponsavelCliente,
                DataRegistoEncomenda = encomendaMoldeAtual?.Encomenda?.DataRegisto,
                DataEntregaPrevista = encomendaMoldeAtual?.DataEntregaPrevista,
                TotalPecas = pecas.Count,
                MaterialPendente = pecas.Count(p => !p.MaterialRecebido),
                TotalProjetos = projetos.Count,
                TotalRevisoes = totalRevisoes,
                UltimaRevisaoEm = ultimaRevisaoEm,
                Maquinacao = maquinacao,
                Erosao = erosao,
                Montagem = montagem,
                EmEspera = emEspera,
                EmTrabalho = emTrabalho,
                Concluidas = concluidas,
                PercentagemConclusao = pecas.Count == 0
                    ? 0
                    : Math.Round((decimal)concluidas / pecas.Count * 100m, 2),
                Projetos = projetos,
                Fases =
                [
                    new MoldeFaseResumoDto { NomeFase = NomeFases.MAQUINACAO.ToString(), PecasComMovimento = maquinacao },
                    new MoldeFaseResumoDto { NomeFase = NomeFases.EROSAO.ToString(), PecasComMovimento = erosao },
                    new MoldeFaseResumoDto { NomeFase = NomeFases.MONTAGEM.ToString(), PecasComMovimento = montagem }
                ]
            };

            return dto;
        }

        /// <summary>
        /// Obtem o contexto base usado no preenchimento da ficha FLT.
        /// </summary>
        /// <remarks>
        /// A FLT nao depende de uma ficha editavel persistida.
        /// O documento e gerado diretamente a partir do contexto Encomenda-Molde e dos dados
        /// tecnicos ja registados no sistema.
        /// </remarks>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada para gerar a FLT.</param>
        /// <returns>Read-model base da FLT ou nulo quando o contexto nao existe.</returns>
        public Task<FichaRelatorioBaseDto?> ObterFltRelatorioBaseAsync(int encomendaMoldeId)
        {
            return _context.EncomendasMoldes
                .AsNoTracking()
                .Where(em => em.EncomendaMolde_id == encomendaMoldeId)
                .Select(FltRelatorioBaseProjection)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtem o contexto base usado no preenchimento das fichas editaveis exportadas.
        /// </summary>
        /// <remarks>
        /// Esta query devolve apenas o shape necessario ao documento para evitar acoplamento
        /// entre a geracao do Excel e o modelo de persistencia completo.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha editavel.</param>
        /// <returns>Read-model base da ficha ou nulo quando a ficha nao existe.</returns>
        public Task<FichaRelatorioBaseDto?> ObterFichaRelatorioBaseAsync(int fichaId)
        {
            return _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.FichaProducao_id == fichaId)
                .Select(FichaRelatorioBaseProjection)
                .FirstOrDefaultAsync();
        }

        public Task<FichaFrmRelatorioDto?> ObterFichaFrmRelatorioAsync(int fichaId)
        {
            return _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.FichaProducao_id == fichaId && f.Tipo == TipoFicha.FRM)
                .Select(f => new FichaFrmRelatorioDto
                {
                    Base = new FichaRelatorioBaseDto
                    {
                        FichaId = f.FichaProducao_id,
                        Tipo = f.Tipo,
                        MoldeNumero = f.EncomendaMolde!.Molde!.Numero,
                        MoldeNome = f.EncomendaMolde.Molde.Nome,
                        NumeroMoldeCliente = f.EncomendaMolde.Molde.NumeroMoldeCliente,
                        ClienteNome = f.EncomendaMolde.Encomenda!.Cliente!.Nome,
                        NomeServicoCliente = f.EncomendaMolde.Encomenda.NomeServicoCliente,
                        NumeroProjetoCliente = f.EncomendaMolde.Encomenda.NumeroProjetoCliente,
                        NomeResponsavelCliente = f.EncomendaMolde.Encomenda.NomeResponsavelCliente,
                    },
                    Linhas = _context.FichasFrmLinhas
                        .Where(l => l.FichaProducao_id == f.FichaProducao_id)
                        .OrderBy(l => l.Data)
                        .ThenBy(l => l.FichaFrmLinha_id)
                        .Select(l => new FichaFrmRelatorioLinhaDto
                        {
                            Data = l.Data,
                            Defeito = l.Defeito,
                            Pormenor = l.Pormenor,
                            Verificado = l.Verificado,
                            ResponsavelNome = _context.Users
                                .Where(u => u.User_id == l.Responsavel_id)
                                .Select(u => u.Nome)
                                .FirstOrDefault() ?? $"User {l.Responsavel_id}"
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public Task<FichaFraRelatorioDto?> ObterFichaFraRelatorioAsync(int fichaId)
        {
            return _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.FichaProducao_id == fichaId && f.Tipo == TipoFicha.FRA)
                .Select(f => new FichaFraRelatorioDto
                {
                    Base = new FichaRelatorioBaseDto
                    {
                        FichaId = f.FichaProducao_id,
                        Tipo = f.Tipo,
                        MoldeNumero = f.EncomendaMolde!.Molde!.Numero,
                        MoldeNome = f.EncomendaMolde.Molde.Nome,
                        NumeroMoldeCliente = f.EncomendaMolde.Molde.NumeroMoldeCliente,
                        ClienteNome = f.EncomendaMolde.Encomenda!.Cliente!.Nome,
                        NomeServicoCliente = f.EncomendaMolde.Encomenda.NomeServicoCliente,
                        NumeroProjetoCliente = f.EncomendaMolde.Encomenda.NumeroProjetoCliente,
                        NomeResponsavelCliente = f.EncomendaMolde.Encomenda.NomeResponsavelCliente,
                    },
                    Linhas = _context.FichasFraLinhas
                        .Where(l => l.FichaProducao_id == f.FichaProducao_id)
                        .OrderBy(l => l.Data)
                        .ThenBy(l => l.FichaFraLinha_id)
                        .Select(l => new FichaFraRelatorioLinhaDto
                        {
                            Data = l.Data,
                            Alteracoes = l.Alteracoes,
                            Verificado = l.Verificado,
                            ResponsavelNome = _context.Users
                                .Where(u => u.User_id == l.Responsavel_id)
                                .Select(u => u.Nome)
                                .FirstOrDefault() ?? $"User {l.Responsavel_id}"
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public Task<FichaFopRelatorioDto?> ObterFichaFopRelatorioAsync(int fichaId)
        {
            return _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.FichaProducao_id == fichaId && f.Tipo == TipoFicha.FOP)
                .Select(f => new FichaFopRelatorioDto
                {
                    Base = new FichaRelatorioBaseDto
                    {
                        FichaId = f.FichaProducao_id,
                        Tipo = f.Tipo,
                        MoldeNumero = f.EncomendaMolde!.Molde!.Numero,
                        MoldeNome = f.EncomendaMolde.Molde.Nome,
                        NumeroMoldeCliente = f.EncomendaMolde.Molde.NumeroMoldeCliente,
                        ClienteNome = f.EncomendaMolde.Encomenda!.Cliente!.Nome,
                        NomeServicoCliente = f.EncomendaMolde.Encomenda.NomeServicoCliente,
                        NumeroProjetoCliente = f.EncomendaMolde.Encomenda.NumeroProjetoCliente,
                        NomeResponsavelCliente = f.EncomendaMolde.Encomenda.NomeResponsavelCliente,
                    },
                    Linhas = _context.FichasFopLinhas
                        .Where(l => l.FichaProducao_id == f.FichaProducao_id)
                        .OrderBy(l => l.Data)
                        .ThenBy(l => l.FichaFopLinha_id)
                        .Select(l => new FichaFopRelatorioLinhaDto
                        {
                            Data = l.Data,
                            Ocorrencia = l.Ocorrencia,
                            Correcao = l.Correcao,
                            ResponsavelNome = _context.Users
                                .Where(u => u.User_id == l.Responsavel_id)
                                .Select(u => u.Nome)
                                .FirstOrDefault() ?? $"User {l.Responsavel_id}"
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResult<FichaFopGeralRelatorioLinhaDto>> ObterFopGeralAsync(DateTime dataInicio, DateTime dataFim, int page, int pageSize)
        {
            var inicio = dataInicio.Date;
            var fim = dataFim.Date;

            var query = _context.FichasFopLinhas
                .AsNoTracking()
                .Where(l => l.Data.Date >= inicio && l.Data.Date <= fim)
                .OrderBy(l => l.Data)
                .ThenBy(l => l.FichaFopLinha_id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new FichaFopGeralRelatorioLinhaDto
                {
                    FichaFopLinha_id = l.FichaFopLinha_id,
                    FichaFop_id = l.FichaProducao_id,
                    EncomendaMolde_id = l.FichaProducao!.EncomendaMolde_id,
                    Data = l.Data,
                    Ocorrencia = l.Ocorrencia,
                    Correcao = l.Correcao,
                    ResponsavelNome = _context.Users
                        .Where(u => u.User_id == l.Responsavel_id)
                        .Select(u => u.Nome)
                        .FirstOrDefault() ?? $"User {l.Responsavel_id}",
                    Peca_id = l.Peca_id,
                    PecaNumero = l.Peca != null ? l.Peca.NumeroPeca : null,
                    PecaDesignacao = l.Peca != null ? l.Peca.Designacao : null,
                    Molde_id = l.Molde_id,
                    MoldeNumero = l.Molde != null ? l.Molde.Numero : l.FichaProducao!.EncomendaMolde!.Molde!.Numero,
                    MoldeNome = l.Molde != null ? l.Molde.Nome : l.FichaProducao.EncomendaMolde!.Molde!.Nome
                })
                .ToListAsync();

            return new PagedResult<FichaFopGeralRelatorioLinhaDto>(items, totalCount, page, pageSize);
        }
    }
}

