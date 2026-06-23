using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.OcorrenciaDto;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Ocorrencias;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa o registo independente de ocorrencias e correcoes na FOP.
    /// </summary>
    public class OcorrenciasService : IOcorrenciasService
    {
        private readonly IFichaProducaoService _fichaProducaoService;
        private readonly IEncomendaMoldeRepository _encomendaMoldeRepository;
        private readonly IPecaRepository _pecaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OcorrenciasService> _logger;

        public OcorrenciasService(
            IFichaProducaoService fichaProducaoService,
            IEncomendaMoldeRepository encomendaMoldeRepository,
            IPecaRepository pecaRepository,
            IUserRepository userRepository,
            ILogger<OcorrenciasService> logger)
        {
            _fichaProducaoService = fichaProducaoService;
            _encomendaMoldeRepository = encomendaMoldeRepository;
            _pecaRepository = pecaRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ResponseFichaFopLinhaDto> CreateAsync(CreateOcorrenciaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Ocorrencia))
                throw new ArgumentException("A ocorrencia e obrigatoria.");

            var encomendaMolde = await _encomendaMoldeRepository.GetByIdAsync(dto.EncomendaMolde_id)
                ?? throw new KeyNotFoundException($"EncomendaMolde com ID {dto.EncomendaMolde_id} nao encontrado.");

            var peca = await _pecaRepository.GetByIdAsync(dto.Peca_id)
                ?? throw new KeyNotFoundException($"Peca com ID {dto.Peca_id} nao encontrada.");

            if (peca.Molde_id != encomendaMolde.Molde_id)
                throw new ArgumentException("A peca nao pertence ao molde associado a esta encomenda.");

            if (await _userRepository.GetByIdAsync(dto.Responsavel_id) == null)
                throw new KeyNotFoundException($"Responsavel com ID {dto.Responsavel_id} nao encontrado.");

            var fichaFop = await _fichaProducaoService.EnsureAsync(new CreateFichaProducaoDto
            {
                Tipo = TipoFicha.FOP,
                EncomendaMolde_id = dto.EncomendaMolde_id
            });

            var created = await _fichaProducaoService.CreateLinhaFopAsync(
                fichaFop.FichaProducao_id,
                new CreateFichaFopLinhaDto
                {
                    Data = DateTime.UtcNow,
                    Ocorrencia = dto.Ocorrencia.Trim(),
                    Correcao = string.IsNullOrWhiteSpace(dto.Correcao) ? null : dto.Correcao.Trim(),
                    Responsavel_id = dto.Responsavel_id,
                    Peca_id = dto.Peca_id,
                    Molde_id = peca.Molde_id
                });

            _logger.LogInformation(
                "Ocorrencia {OcorrenciaId} registada para a peca {PecaId} na FOP {FopId}.",
                created.FichaFopLinha_id,
                created.Peca_id,
                created.FichaFop_id);

            return created;
        }
    }
}
