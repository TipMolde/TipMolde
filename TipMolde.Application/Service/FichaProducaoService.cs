using AutoMapper;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;
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

            switch (ficha.Tipo)
            {
                case TipoFicha.FRM:
                    dto.LinhasFrm = await LoadAllLinhasFrmAsync(id);
                    break;
                case TipoFicha.FRA:
                    dto.LinhasFra = await LoadAllLinhasFraAsync(id);
                    break;
                case TipoFicha.FOP:
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

            var ficha = _mapper.Map<FichaProducao>(dto);
            ficha.Tipo = dto.Tipo;
            ficha.DataCriacao = DateTime.UtcNow;

            var created = await _fichaRepository.AddAsync(ficha);
            return _mapper.Map<ResponseFichaProducaoDto>(created);
        }

        public async Task<PagedResult<ResponseFichaFrmLinhaDto>> GetLinhasFrmAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FRM);

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
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FRM);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFrmLinha>(dto);
            linha.FichaProducao_id = fichaId;
            return _mapper.Map<ResponseFichaFrmLinhaDto>(await _fichaRepository.AddLinhaFrmAsync(linha));
        }

        public async Task<PagedResult<ResponseFichaFraLinhaDto>> GetLinhasFraAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FRA);

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
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FRA);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFraLinha>(dto);
            linha.FichaProducao_id = fichaId;
            return _mapper.Map<ResponseFichaFraLinhaDto>(await _fichaRepository.AddLinhaFraAsync(linha));
        }

        public async Task<PagedResult<ResponseFichaFopLinhaDto>> GetLinhasFopAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FOP);

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
            await EnsureFichaTipoAsync(fichaId, TipoFicha.FOP);
            await EnsureUserExistsAsync(dto.Responsavel_id, ResponsavelDescricao);

            var linha = _mapper.Map<FichaFopLinha>(dto);
            linha.FichaProducao_id = fichaId;
            return _mapper.Map<ResponseFichaFopLinhaDto>(await _fichaRepository.AddLinhaFopAsync(linha));
        }

        private async Task<FichaProducao> EnsureFichaTipoAsync(int fichaId, TipoFicha tipoEsperado)
        {
            var ficha = await _fichaRepository.GetByIdAsync(fichaId)
                ?? throw new KeyNotFoundException($"Ficha de producao com ID {fichaId} nao encontrada.");

            if (ficha.Tipo != tipoEsperado)
                throw new ArgumentException($"A ficha {fichaId} nao corresponde ao tipo esperado {tipoEsperado}.");

            return ficha;
        }


        private async Task EnsureUserExistsAsync(int userId, string descricao)
        {
            if (await _userRepository.GetByIdAsync(userId) == null)
                throw new KeyNotFoundException($"{descricao} com ID {userId} nao encontrado.");
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
