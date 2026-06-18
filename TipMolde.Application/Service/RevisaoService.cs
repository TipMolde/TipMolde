using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.RevisaoDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRevisao;
using TipMolde.Domain.Entities.Desenho;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da feature Revisao.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de negocio, mapping entre Dtos e dominio
    /// e regras de rastreabilidade da resposta do cliente.
    /// </remarks>
    public class RevisaoService : IRevisaoService
    {
        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".docx",
            ".png",
            ".jpg",
            ".jpeg"
        };

        private static readonly HashSet<string> AllowedAttachmentContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/png",
            "image/jpeg"
        };

        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;

        private readonly IRevisaoRepository _revisaoRepository;
        private readonly IProjetoRepository _projetoRepository;
        private readonly IRevisaoAttachmentStorage _attachmentStorage;
        private readonly IMapper _mapper;
        private readonly ILogger<RevisaoService> _logger;

        /// <summary>
        /// Construtor de RevisaoService.
        /// </summary>
        /// <param name="revisaoRepository">Repositorio do agregado Revisao.</param>
        /// <param name="projetoRepository">Repositorio usado para validar o projeto referenciado.</param>
        /// <param name="mapper">Mapper para conversao entre Dtos e entidade de dominio.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public RevisaoService(
            IRevisaoRepository revisaoRepository,
            IProjetoRepository projetoRepository,
            IRevisaoAttachmentStorage attachmentStorage,
            IMapper mapper,
            ILogger<RevisaoService> logger)
        {
            _revisaoRepository = revisaoRepository;
            _projetoRepository = projetoRepository;
            _attachmentStorage = attachmentStorage;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista revisoes associadas a um projeto.
        /// </summary>
        /// <param name="projetoId">Identificador do projeto.</param>
        /// <param name="page">Numero da pagina a ser retornada.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Colecao de Dtos de revisao ordenados por numero decrescente.</returns>
        public async Task<PagedResult<ResponseRevisaoDto>> GetByProjetoIdAsync(int projetoId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _revisaoRepository.GetByProjetoIdAsync(projetoId, normalizedPage, normalizedPageSize);
            var itens = _mapper.Map<IEnumerable<ResponseRevisaoDto>>(result.Items);
            return new PagedResult<ResponseRevisaoDto>(itens, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem uma revisao por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da revisao.</param>
        /// <returns>DTO da revisao quando encontrada; nulo caso contrario.</returns>
        public async Task<ResponseRevisaoDto?> GetByIdAsync(int id)
        {
            var revisao = await _revisaoRepository.GetByIdAsync(id);
            return revisao == null ? null : _mapper.Map<ResponseRevisaoDto>(revisao);
        }

        /// <summary>
        /// Cria uma nova revisao para um projeto existente.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida descricao obrigatoria.
        /// 2. Valida a existencia do projeto.
        /// 3. Persiste a revisao com numeracao consistente por projeto.
        /// </remarks>
        /// <param name="dto">Dados de criacao da revisao.</param>
        /// <returns>DTO da revisao criada.</returns>
        public async Task<ResponseRevisaoDto> CreateAsync(CreateRevisaoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DescricaoAlteracoes))
                throw new ArgumentException("Descricao das alteracoes e obrigatoria.");

            var projeto = await _projetoRepository.GetWithRevisoesAsync(dto.Projeto_id);
            if (projeto == null)
                throw new KeyNotFoundException($"Projeto com ID {dto.Projeto_id} nao encontrado.");

            var ultimaRevisao = projeto.Revisoes?
                .OrderByDescending(item => item.NumRevisao)
                .FirstOrDefault();

            if (ultimaRevisao is not null)
            {
                if (!ultimaRevisao.DataResposta.HasValue)
                    throw new BusinessConflictException("Nao e possivel criar uma nova revisao enquanto existir uma revisao em aberto para este projeto.");

                if (ultimaRevisao.Aprovado == true)
                    throw new BusinessConflictException("Nao e possivel criar uma nova revisao depois de o cliente ter aprovado a ultima revisao.");
            }

            var revisao = _mapper.Map<Revisao>(dto);
            revisao.DataEnvioCliente = DateTime.UtcNow;

            // Porque: `NumRevisao` tem de ser unico por projeto e a geracao do proximo numero
            // nao deve ficar separada da persistencia para evitar colisoes concorrentes.
            var created = await _revisaoRepository.AddWithGeneratedNumeroAsync(revisao);

            _logger.LogInformation(
                "Revisao {RevisaoId} criada para o projeto {ProjetoId} com numero {NumRevisao}",
                created.Revisao_id,
                created.Projeto_id,
                created.NumRevisao);

            return _mapper.Map<ResponseRevisaoDto>(created);
        }

        /// <summary>
        /// Regista a primeira resposta do cliente a uma revisao enviada.
        /// </summary>
        /// <remarks>
        /// Regras criticas:
        /// 1. A decisao do cliente so pode ser gravada uma vez.
        /// 2. Rejeicoes devem guardar justificacao textual ou evidencia anexada.
        /// 3. A data de resposta deve refletir a primeira decisao efetiva do cliente.
        /// </remarks>
        /// <param name="revisaoId">Identificador da revisao.</param>
        /// <param name="dto">Payload de resposta do cliente.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task UpdateRespostaClienteAsync(int revisaoId, UpdateRespostaRevisaoDto dto)
        {
            await UpdateRespostaClienteAsync(revisaoId, dto, null, null, null);
        }

        /// <summary>
        /// Regista a primeira resposta do cliente a uma revisao enviada com anexo opcional.
        /// </summary>
        /// <param name="revisaoId">Identificador da revisao.</param>
        /// <param name="dto">Payload de resposta do cliente.</param>
        /// <param name="attachmentContent">Conteudo binario do anexo submetido.</param>
        /// <param name="attachmentFileName">Nome original do anexo submetido.</param>
        /// <param name="attachmentContentType">Tipo MIME do anexo submetido.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task UpdateRespostaClienteAsync(
            int revisaoId,
            UpdateRespostaRevisaoDto dto,
            byte[]? attachmentContent,
            string? attachmentFileName,
            string? attachmentContentType)
        {
            var existing = await _revisaoRepository.GetByIdAsync(revisaoId);
            if (existing == null)
                throw new KeyNotFoundException($"Revisao com ID {revisaoId} nao encontrada.");

            if (existing.Aprovado.HasValue || existing.DataResposta.HasValue)
                throw new BusinessConflictException("A resposta do cliente ja foi registada para esta revisao.");

            if (!dto.Aprovado.HasValue)
                throw new ArgumentException("O campo Aprovado deve ser enviado explicitamente.");

            var feedbackTexto = NormalizeOptionalText(dto.FeedbackTexto);
            var feedbackImagemPath = NormalizeOptionalText(dto.FeedbackImagemPath);
            string? attachmentPath = null;

            if (dto.Aprovado == true
                && (attachmentContent is not null || !string.IsNullOrWhiteSpace(feedbackImagemPath)))
            {
                throw new ArgumentException("Uma revisao aprovada nao pode incluir anexos ou imagem de feedback.");
            }

            try
            {
                if (attachmentContent is not null)
                {
                    ValidateAttachment(attachmentContent, attachmentFileName, attachmentContentType);
                    attachmentPath = await _attachmentStorage.SaveAsync(revisaoId, attachmentFileName!, attachmentContent);
                }

                if (dto.Aprovado == false
                    && string.IsNullOrWhiteSpace(feedbackTexto)
                    && string.IsNullOrWhiteSpace(feedbackImagemPath)
                    && string.IsNullOrWhiteSpace(attachmentPath))
                {
                    throw new ArgumentException("Uma revisao rejeitada deve incluir FeedbackTexto, FeedbackImagemPath ou um anexo.");
                }

                existing.Aprovado = dto.Aprovado.Value;
                existing.DataResposta = DateTime.UtcNow;
                existing.FeedbackTexto = feedbackTexto;
                existing.FeedbackImagemPath = !string.IsNullOrWhiteSpace(attachmentPath)
                    ? attachmentPath
                    : feedbackImagemPath;

                await _revisaoRepository.UpdateAsync(existing);
            }
            catch
            {
                await _attachmentStorage.DeleteIfExistsAsync(attachmentPath);
                throw;
            }

            _logger.LogInformation(
                "Resposta do cliente registada para a revisao {RevisaoId}. Aprovado: {Aprovado}",
                revisaoId,
                existing.Aprovado);
        }

        /// <summary>
        /// Remove uma revisao existente.
        /// </summary>
        /// <param name="id">Identificador da revisao a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var existing = await _revisaoRepository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Revisao com ID {id} nao encontrada.");

            await _revisaoRepository.DeleteAsync(id);

            _logger.LogInformation("Revisao {RevisaoId} removida com sucesso", id);
        }

        /// <summary>
        /// Normaliza campos textuais opcionais do payload.
        /// </summary>
        /// <param name="value">Valor textual recebido.</param>
        /// <returns>Texto trimado ou nulo quando o valor chega vazio.</returns>
        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Valida o anexo submetido para uma resposta de revisao.
        /// </summary>
        private static void ValidateAttachment(byte[] content, string? fileName, string? contentType)
        {
            if (content.Length <= 0)
                throw new ArgumentException("O anexo da revisao nao pode estar vazio.");

            if (content.LongLength > MaxAttachmentSizeBytes)
                throw new ArgumentException($"O anexo excede o limite maximo de {MaxAttachmentSizeBytes / (1024 * 1024)} MB.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("O nome do anexo e invalido.");

            var safeOriginalFileName = Path.GetFileName(fileName);

            var extension = Path.GetExtension(safeOriginalFileName);
            if (!AllowedAttachmentExtensions.Contains(extension))
                throw new ArgumentException("A extensao do anexo nao e suportada.");

            if (string.IsNullOrWhiteSpace(contentType) || !AllowedAttachmentContentTypes.Contains(contentType))
                throw new ArgumentException("O tipo MIME do anexo nao e suportado.");
        }
    }
}
