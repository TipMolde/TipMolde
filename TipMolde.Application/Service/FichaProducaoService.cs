using AutoMapper;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas.Linhas;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso das fichas editaveis de producao.
    /// </summary>
    public class FichaProducaoService : IFichaProducaoService
    {
        private const string ResponsavelDescricao = "Responsavel";

        private readonly IFichaProducaoRepository _fichaRepository;
        private readonly IEncomendaMoldeRepository _encomendaMoldeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Construtor de FichaProducaoService.
        /// </summary>
        /// <param name="fichaRepository">Repositorio principal das fichas editaveis.</param>
        /// <param name="encomendaMoldeRepository">Repositorio usado para validar o contexto Encomenda-Molde.</param>
        /// <param name="userRepository">Repositorio usado para validar utilizadores envolvidos nas operacoes.</param>
        /// <param name="mapper">Mapper responsavel pela conversao entre entidades e DTOs.</param>
        public FichaProducaoService(
            IFichaProducaoRepository fichaRepository,
            IEncomendaMoldeRepository encomendaMoldeRepository,
            IUserRepository userRepository,
            AutoMapper.IMapper mapper)
        {
            _fichaRepository = fichaRepository;
            _encomendaMoldeRepository = encomendaMoldeRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ResponseFichaProducaoDto>> GetByEncomendaMoldeIdAsync(int encomendaMoldeId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var fichas = await _fichaRepository.GetByEncomendaMoldeIdAsync(encomendaMoldeId, normalizedPage, normalizedPageSize);
            return new PagedResult<ResponseFichaProducaoDto>(
                _mapper.Map<IEnumerable<ResponseFichaProducaoDto>>(fichas.Items),
                fichas.TotalCount,
                fichas.CurrentPage,
                fichas.PageSize);
        }

        public async Task<PagedResult<ResponseFichaProducaoDto>> GetByMoldeIdAsync(int moldeId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var fichas = await _fichaRepository.GetByMoldeIdAsync(moldeId, normalizedPage, normalizedPageSize);
            return new PagedResult<ResponseFichaProducaoDto>(
                _mapper.Map<IEnumerable<ResponseFichaProducaoDto>>(fichas.Items),
                fichas.TotalCount,
                fichas.CurrentPage,
                fichas.PageSize);
        }

        /// <summary>
        /// Obtem o detalhe completo de uma ficha editavel.
        /// </summary>
        /// <remarks>
        /// Para evitar truncagem silenciosa, este metodo carrega todas as linhas manuais
        /// atualmente existentes nas fichas FRM, FRA e FOP.
        /// </remarks>
        /// <param name="id">Identificador interno da ficha editavel.</param>
        /// <returns>DTO detalhado da ficha ou nulo quando a ficha nao existe.</returns>
        public async Task<ResponseFichaProducaoDetalheDto?> GetByIdAsync(int id)
        {
            var ficha = await _fichaRepository.GetByIdDetalheAsync(id);
            if (ficha == null)
                return null;

            var dto = _mapper.Map<ResponseFichaProducaoDetalheDto>(ficha);

            switch (ficha)
            {
                case FichaFrm:
                    dto.LinhasFrm = await LoadAllLinhasFrmAsync(id);
                    break;
                case FichaFra:
                    dto.LinhasFra = await LoadAllLinhasFraAsync(id);
                    break;
                case FichaFop:
                    dto.LinhasFop = await LoadAllLinhasFopAsync(id);
                    break;
            }

            return dto;
        }

        public async Task<ResponseFichaProducaoDto> CreateAsync(CreateFichaProducaoDto dto)
        {
            if (dto.Tipo == TipoFicha.FLT)
                throw new ArgumentException("A ficha FLT e gerada diretamente pelos dados do sistema e nao pode ser criada manualmente.");

            if (await _encomendaMoldeRepository.GetByIdAsync(dto.EncomendaMolde_id) == null)
                throw new KeyNotFoundException($"EncomendaMolde com ID {dto.EncomendaMolde_id} nao encontrado.");

            FichaProducao ficha = dto.Tipo switch
            {
                TipoFicha.FRE => _mapper.Map<FichaFre>(dto),
                TipoFicha.FRM => _mapper.Map<FichaFrm>(dto),
                TipoFicha.FRA => _mapper.Map<FichaFra>(dto),
                TipoFicha.FOP => _mapper.Map<FichaFop>(dto),
                _ => throw new ArgumentException("Tipo de ficha nao suportado.")
            };

            ficha.Tipo = dto.Tipo;
            ficha.DataCriacao = DateTime.UtcNow;
            ficha.Estado = EstadoFichaProducao.RASCUNHO;
            ficha.Ativa = true;

            var created = await _fichaRepository.AddAsync(ficha);
            return _mapper.Map<ResponseFichaProducaoDto>(created);
        }

        public async Task<ResponseFichaProducaoDto> SubmitAsync(int id, int userId)
        {
            var ficha = await GetFichaAtivaAsync(id);
            EnsureFichaNaoTerminada(ficha);
            await EnsureUserExistsAsync(userId, "Utilizador de submissao");
            await ValidarConteudoMinimoParaSubmissaoAsync(ficha);

            if (ficha.Estado == EstadoFichaProducao.SUBMETIDA)
                throw new ArgumentException("A ficha ja se encontra submetida.");

            ficha.Estado = EstadoFichaProducao.SUBMETIDA;
            ficha.SubmetidaEm = DateTime.UtcNow;
            ficha.SubmetidaPor_user_id = userId;

            await _fichaRepository.UpdateAsync(ficha);
            return _mapper.Map<ResponseFichaProducaoDto>(ficha);
        }

        public async Task<ResponseFichaProducaoDto> CancelAsync(int id, int userId)
        {
            var ficha = await GetFichaAtivaAsync(id);
            await EnsureUserExistsAsync(userId, "Utilizador de cancelamento");

            ficha.Ativa = false;
            ficha.Estado = EstadoFichaProducao.CANCELADA;
            ficha.DesativadaEm = DateTime.UtcNow;
            ficha.DesativadaPor_user_id = userId;

            await _fichaRepository.UpdateAsync(ficha);
            return _mapper.Map<ResponseFichaProducaoDto>(ficha);
        }

        public async Task<PagedResult<ResponseFichaFrmLinhaDto>> GetLinhasFrmAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            if (ficha.Tipo != TipoFicha.FRM)
                throw new ArgumentException($"A ficha {fichaId} nao corresponde ao tipo esperado FRM.");

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var linhas = await _fichaRepository.GetLinhasFrmByFichaIdAsync(fichaId, normalizedPage, normalizedPageSize);
            return new PagedResult<ResponseFichaFrmLinhaDto>(
                _mapper.Map<IEnumerable<ResponseFichaFrmLinhaDto>>(linhas.Items),
                linhas.TotalCount,
                linhas.CurrentPage,
                linhas.PageSize);
        }

        public async Task<ResponseFichaFrmLinhaDto> CreateLinhaFrmAsync(int fichaId, CreateFichaFrmLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FRM);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFrmLinha>(dto);
            linha.FichaFrm_id = fichaId;
            return _mapper.Map<ResponseFichaFrmLinhaDto>(await _fichaRepository.AddLinhaFrmAsync(linha));
        }

        public async Task<ResponseFichaFrmLinhaDto> UpdateLinhaFrmAsync(int fichaId, int linhaId, CreateFichaFrmLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FRM);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = await _fichaRepository.GetLinhaFrmByIdAsync(fichaId, linhaId)
                ?? throw new KeyNotFoundException($"Linha FRM {linhaId} nao encontrada.");

            _mapper.Map(dto, linha);
            await _fichaRepository.UpdateLinhaFrmAsync(linha);
            return _mapper.Map<ResponseFichaFrmLinhaDto>(linha);
        }

        public async Task<PagedResult<ResponseFichaFraLinhaDto>> GetLinhasFraAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            if (ficha.Tipo != TipoFicha.FRA)
                throw new ArgumentException($"A ficha {fichaId} nao corresponde ao tipo esperado FRA.");

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var linhas = await _fichaRepository.GetLinhasFraByFichaIdAsync(fichaId, normalizedPage, normalizedPageSize);
            return new PagedResult<ResponseFichaFraLinhaDto>(
                _mapper.Map<IEnumerable<ResponseFichaFraLinhaDto>>(linhas.Items),
                linhas.TotalCount,
                linhas.CurrentPage,
                linhas.PageSize);
        }

        public async Task<ResponseFichaFraLinhaDto> CreateLinhaFraAsync(int fichaId, CreateFichaFraLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FRA);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFraLinha>(dto);
            linha.FichaFra_id = fichaId;
            return _mapper.Map<ResponseFichaFraLinhaDto>(await _fichaRepository.AddLinhaFraAsync(linha));
        }

        public async Task<ResponseFichaFraLinhaDto> UpdateLinhaFraAsync(int fichaId, int linhaId, CreateFichaFraLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FRA);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = await _fichaRepository.GetLinhaFraByIdAsync(fichaId, linhaId)
                ?? throw new KeyNotFoundException($"Linha FRA {linhaId} nao encontrada.");

            _mapper.Map(dto, linha);
            await _fichaRepository.UpdateLinhaFraAsync(linha);
            return _mapper.Map<ResponseFichaFraLinhaDto>(linha);
        }

        public async Task<PagedResult<ResponseFichaFopLinhaDto>> GetLinhasFopAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            if (ficha.Tipo != TipoFicha.FOP)
                throw new ArgumentException($"A ficha {fichaId} nao corresponde ao tipo esperado FOP.");

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var linhas = await _fichaRepository.GetLinhasFopByFichaIdAsync(fichaId, normalizedPage, normalizedPageSize);
            return new PagedResult<ResponseFichaFopLinhaDto>(
                _mapper.Map<IEnumerable<ResponseFichaFopLinhaDto>>(linhas.Items),
                linhas.TotalCount,
                linhas.CurrentPage,
                linhas.PageSize);
        }

        public async Task<ResponseFichaFopLinhaDto> CreateLinhaFopAsync(int fichaId, CreateFichaFopLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FOP);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFopLinha>(dto);
            linha.FichaFop_id = fichaId;
            return _mapper.Map<ResponseFichaFopLinhaDto>(await _fichaRepository.AddLinhaFopAsync(linha));
        }

        public async Task<ResponseFichaFopLinhaDto> UpdateLinhaFopAsync(int fichaId, int linhaId, CreateFichaFopLinhaDto dto)
        {
            var ficha = await GetFichaAtivaAsync(fichaId);
            EnsureFichaEditavel(ficha, TipoFicha.FOP);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = await _fichaRepository.GetLinhaFopByIdAsync(fichaId, linhaId)
                ?? throw new KeyNotFoundException($"Linha FOP {linhaId} nao encontrada.");

            _mapper.Map(dto, linha);
            await _fichaRepository.UpdateLinhaFopAsync(linha);
            return _mapper.Map<ResponseFichaFopLinhaDto>(linha);
        }

        private async Task<FichaProducao> GetFichaAtivaAsync(int fichaId)
        {
            var ficha = await _fichaRepository.GetByIdAsync(fichaId)
                ?? throw new KeyNotFoundException($"Ficha de producao com ID {fichaId} nao encontrada.");

            if (!ficha.Ativa)
                throw new ArgumentException("A ficha indicada encontra-se inativa.");

            return ficha;
        }

        private static void EnsureFichaNaoTerminada(FichaProducao ficha)
        {
            if (ficha.Estado is EstadoFichaProducao.CANCELADA or EstadoFichaProducao.FECHADA)
                throw new ArgumentException("A ficha indicada nao se encontra editavel.");
        }

        private static void EnsureFichaEditavel(FichaProducao ficha, TipoFicha tipoEsperado)
        {
            if (ficha.Tipo != tipoEsperado)
                throw new ArgumentException($"A ficha {ficha.FichaProducao_id} nao corresponde ao tipo esperado {tipoEsperado}.");

            EnsureFichaNaoTerminada(ficha);

            if (ficha.Estado == EstadoFichaProducao.SUBMETIDA)
                throw new ArgumentException("A ficha ja foi submetida e nao pode ser alterada.");
        }

        private async Task EnsureUserExistsAsync(int userId, string descricao)
        {
            if (await _userRepository.GetByIdAsync(userId) == null)
                throw new KeyNotFoundException($"{descricao} com ID {userId} nao encontrado.");
        }

        private async Task ValidarConteudoMinimoParaSubmissaoAsync(FichaProducao ficha)
        {
            switch (ficha)
            {
                case FichaFrm:
                    if ((await _fichaRepository.GetLinhasFrmByFichaIdAsync(ficha.FichaProducao_id, 1, 1)).TotalCount == 0)
                        throw new ArgumentException("A ficha FRM precisa de pelo menos uma linha de melhoria antes de ser submetida.");
                    break;
                case FichaFra:
                    if ((await _fichaRepository.GetLinhasFraByFichaIdAsync(ficha.FichaProducao_id, 1, 1)).TotalCount == 0)
                        throw new ArgumentException("A ficha FRA precisa de pelo menos uma linha de alteracao antes de ser submetida.");
                    break;
                case FichaFop:
                    if ((await _fichaRepository.GetLinhasFopByFichaIdAsync(ficha.FichaProducao_id, 1, 1)).TotalCount == 0)
                        throw new ArgumentException("A ficha FOP precisa de pelo menos uma linha de ocorrencia antes de ser submetida.");
                    break;
            }
        }

        /// <summary>
        /// Carrega todas as linhas manuais da ficha FRM para o detalhe completo.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRM.</param>
        /// <returns>Colecao integral das linhas manuais atualmente persistidas.</returns>
        private async Task<IList<ResponseFichaFrmLinhaDto>> LoadAllLinhasFrmAsync(int fichaId)
        {
            var probe = await _fichaRepository.GetLinhasFrmByFichaIdAsync(fichaId, 1, 1);
            if (probe.TotalCount == 0)
                return new List<ResponseFichaFrmLinhaDto>();

            var linhas = await _fichaRepository.GetLinhasFrmByFichaIdAsync(fichaId, 1, probe.TotalCount);
            return _mapper.Map<IList<ResponseFichaFrmLinhaDto>>(linhas.Items);
        }

        /// <summary>
        /// Carrega todas as linhas manuais da ficha FRA para o detalhe completo.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRA.</param>
        /// <returns>Colecao integral das linhas manuais atualmente persistidas.</returns>
        private async Task<IList<ResponseFichaFraLinhaDto>> LoadAllLinhasFraAsync(int fichaId)
        {
            var probe = await _fichaRepository.GetLinhasFraByFichaIdAsync(fichaId, 1, 1);
            if (probe.TotalCount == 0)
                return new List<ResponseFichaFraLinhaDto>();

            var linhas = await _fichaRepository.GetLinhasFraByFichaIdAsync(fichaId, 1, probe.TotalCount);
            return _mapper.Map<IList<ResponseFichaFraLinhaDto>>(linhas.Items);
        }

        /// <summary>
        /// Carrega todas as linhas manuais da ficha FOP para o detalhe completo.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FOP.</param>
        /// <returns>Colecao integral das linhas manuais atualmente persistidas.</returns>
        private async Task<IList<ResponseFichaFopLinhaDto>> LoadAllLinhasFopAsync(int fichaId)
        {
            var probe = await _fichaRepository.GetLinhasFopByFichaIdAsync(fichaId, 1, 1);
            if (probe.TotalCount == 0)
                return new List<ResponseFichaFopLinhaDto>();

            var linhas = await _fichaRepository.GetLinhasFopByFichaIdAsync(fichaId, 1, probe.TotalCount);
            return _mapper.Map<IList<ResponseFichaFopLinhaDto>>(linhas.Items);
        }
    }
}
