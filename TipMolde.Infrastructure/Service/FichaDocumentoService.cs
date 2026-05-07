using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Domain.Entities.Fichas;

namespace TipMolde.Infrastructure.Service
{
    /// <summary>
    /// Implementa os casos de uso documentais das fichas de producao.
    /// </summary>
    /// <remarks>
    /// Este servico concentra as regras funcionais de versionamento, admissibilidade
    /// e rastreabilidade dos documentos oficiais, delegando armazenamento fisico e
    /// transacoes concretas para adaptadores de infraestrutura.
    /// </remarks>
    public class FichaDocumentoService : IFichaDocumentoService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private readonly IFichaDocumentoRepository _fdRepository;
        private readonly IFichaDocumentoStorage _storage;
        private readonly IFichaDocumentoUnitOfWork _unitOfWork;
        private readonly ILogger<FichaDocumentoService> _logger;
        private readonly IMapper _mapper;

        /// <summary>
        /// Construtor de FichaDocumentoService.
        /// </summary>
        /// <param name="fdRepository">Repositorio responsavel pela persistencia e consulta de metadados documentais.</param>
        /// <param name="storage">Abstracao de infraestrutura responsavel por guardar e ler os ficheiros fisicos.</param>
        /// <param name="unitOfWork">Boundary transacional usado para garantir consistencia entre versoes documentais.</param>
        /// <param name="logger">Logger usado para rastreabilidade operacional e diagnostico de falhas documentais.</param>
        /// <param name="mapper">Mapper responsavel pela conversao entre DTOs e entidades documentais.</param>
        public FichaDocumentoService(
            IFichaDocumentoRepository fdRepository,
            IFichaDocumentoStorage storage,
            IFichaDocumentoUnitOfWork unitOfWork,
            ILogger<FichaDocumentoService> logger,
            IMapper mapper)
        {
            _fdRepository = fdRepository;
            _storage = storage;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Persiste um documento gerado automaticamente pelo sistema.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida a existencia da ficha.
        /// 2. Valida a admissibilidade do documento.
        /// 3. Calcula a proxima versao.
        /// 4. Desativa a versao atualmente ativa.
        /// 5. Persiste o ficheiro fisico com nome versionado.
        /// 6. Regista metadados e hash para auditoria.
        /// 7. Remove o ficheiro fisico se a transacao falhar.
        /// </remarks>
        /// <param name="fichaId">Identificador da ficha de producao dona do documento.</param>
        /// <param name="content">Conteudo binario do ficheiro gerado.</param>
        /// <param name="fileName">Nome base do ficheiro antes da versao final ser aplicada.</param>
        /// <param name="tipoFicheiro">Content type do ficheiro a persistir.</param>
        /// <param name="userId">Utilizador responsavel pela geracao.</param>
        /// <param name="origem">Origem funcional do documento, por exemplo SISTEMA ou UPLOAD.</param>
        /// <returns>DTO seguro com os metadados da nova versao criada.</returns>
        public async Task<ResponseFichaDocumentoDto> GuardarGeradoAsync(
            int fichaId,
            byte[] content,
            string fileName,
            string tipoFicheiro,
            int userId,
            string origem)
        {
            if (!await _fdRepository.FichaExisteAsync(fichaId))
                throw new KeyNotFoundException($"Ficha {fichaId} nao existe.");

            ValidateUploadInput(content, fileName, tipoFicheiro);

            string? finalPath = null;

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var versao = await _fdRepository.GetProximaVersaoAsync(fichaId);
                    await _fdRepository.DesativarVersoesAtivasAsync(fichaId);

                    var nomeFinal = BuildVersionedFileName(fileName, versao);
                    finalPath = await _storage.SaveAsync(fichaId, nomeFinal, content);

                    var createDto = new CreateFichaDocumentoDto
                    {
                        FichaProducao_id = fichaId,
                        CriadoPor_user_id = userId,
                        Versao = versao,
                        Origem = origem,
                        NomeFicheiro = nomeFinal,
                        TipoFicheiro = string.IsNullOrWhiteSpace(tipoFicheiro) ? "application/octet-stream" : tipoFicheiro,
                        CaminhoFicheiro = finalPath,
                        HashSha256 = ComputeSha256(content),
                        Ativo = true
                    };

                    var entidade = _mapper.Map<FichaDocumento>(createDto);

                    await _fdRepository.AddAsync(entidade);

                    _logger.LogInformation(
                        "Documento persistido com sucesso. FichaId={FichaId}, DocumentoId={DocumentoId}, Versao={Versao}",
                        fichaId,
                        entidade.FichaDocumento_id,
                        entidade.Versao);

                    return _mapper.Map<ResponseFichaDocumentoDto>(entidade);
                });
            }
            catch (Exception ex)
            {
                await CleanupFileAsync(finalPath);
                _logger.LogError(ex, "Falha ao persistir documento da ficha {FichaId}.", fichaId);
                throw new InvalidOperationException($"Falha ao persistir metadata do documento da ficha {fichaId}.", ex);
            }
        }

        /// <summary>
        /// Persiste um documento enviado manualmente por um utilizador.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha de producao dona do documento.</param>
        /// <param name="dto">Input normalizado do documento submetido pelo utilizador.</param>
        /// <param name="userId">Identificador do utilizador autenticado.</param>
        /// <returns>DTO seguro com os metadados da nova versao criada.</returns>
        public Task<ResponseFichaDocumentoDto> UploadAsync(int fichaId, UploadFichaDocumentoDto dto, int userId)
        {
            ValidateUploadInput(dto.Content, dto.FileName, dto.ContentType);

            return GuardarGeradoAsync(
                fichaId,
                dto.Content,
                dto.FileName,
                dto.ContentType,
                userId,
                "UPLOAD");
        }

        /// <summary>
        /// Lista todas as versoes documentais associadas a uma ficha.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha de producao.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Colecao paginada de documentos registados para a ficha, sem metadados internos do armazenamento.</returns>
        public async Task<PagedResult<ResponseFichaDocumentoDto>> ListarAsync(int fichaId, int page = 1, int pageSize = 10)
        {
            if (!await _fdRepository.FichaExisteAsync(fichaId))
                throw new KeyNotFoundException($"Ficha {fichaId} nao existe.");

            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

            var docs = await _fdRepository.GetByFichaIdAsync(fichaId, normalizedPage, normalizedPageSize);

            return new PagedResult<ResponseFichaDocumentoDto>(
                _mapper.Map<IEnumerable<ResponseFichaDocumentoDto>>(docs.Items),
                docs.TotalCount,
                docs.CurrentPage,
                docs.PageSize);
        }

        /// <summary>
        /// Carrega o conteudo de um documento persistido pertencente a uma ficha especifica.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha que contextualiza o acesso ao documento.</param>
        /// <param name="documentoId">Identificador interno do documento.</param>
        /// <returns>Conteudo binario, nome final e tipo MIME do ficheiro.</returns>
        public async Task<FichaDocumentoDownloadResultDto> DownloadAsync(int fichaId, int documentoId)
        {
            var doc = await _fdRepository.GetByIdAndFichaIdAsync(fichaId, documentoId)
                ?? throw new KeyNotFoundException("Documento nao encontrado para a ficha indicada.");

            if (!_storage.Exists(doc.CaminhoFicheiro))
                throw new FileNotFoundException($"Ficheiro nao existe no disco: {doc.CaminhoFicheiro}");

            var bytes = await _storage.ReadAsync(doc.CaminhoFicheiro);

            return new FichaDocumentoDownloadResultDto
            {
                Content = bytes,
                FileName = doc.NomeFicheiro,
                TipoFicheiro = doc.TipoFicheiro
            };
        }

        /// <summary>
        /// Constroi o nome final versionado do documento antes da persistencia.
        /// </summary>
        /// <param name="fileName">Nome original do ficheiro submetido ou gerado.</param>
        /// <param name="versao">Numero sequencial da nova versao documental.</param>
        /// <returns>Nome final a persistir e a devolver ao cliente.</returns>
        private static string BuildVersionedFileName(string fileName, int versao)
        {
            var safeOriginalFileName = Path.GetFileName(fileName);
            return $"{Path.GetFileNameWithoutExtension(safeOriginalFileName)}_v{versao}{Path.GetExtension(safeOriginalFileName)}";
        }

        /// <summary>
        /// Valida a admissibilidade minima do documento antes da persistencia.
        /// </summary>
        /// <param name="content">Conteudo binario do ficheiro.</param>
        /// <param name="fileName">Nome original do ficheiro.</param>
        /// <param name="contentType">Tipo MIME indicado para o ficheiro.</param>
        private static void ValidateUploadInput(byte[] content, string fileName, string contentType)
        {
            if (content.Length <= 0)
                throw new ArgumentException("O ficheiro submetido nao pode estar vazio.");

            if (content.LongLength > MaxFileSizeBytes)
                throw new ArgumentException($"O ficheiro excede o limite maximo de {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var safeOriginalFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeOriginalFileName))
                throw new ArgumentException("O nome do ficheiro e invalido.");

            var extension = Path.GetExtension(safeOriginalFileName);
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("A extensao do ficheiro nao e suportada para documentos oficiais da ficha.");

            if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
                throw new ArgumentException("O tipo MIME do ficheiro nao e suportado para documentos oficiais da ficha.");
        }

        /// <summary>
        /// Remove o ficheiro fisico quando a transacao documental falha apos a escrita no disco.
        /// </summary>
        /// <param name="path">Caminho fisico do ficheiro a remover.</param>
        private async Task CleanupFileAsync(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                await _storage.DeleteIfExistsAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nao foi possivel remover o ficheiro apos rollback documental. Path={Path}", path);
            }
        }

        /// <summary>
        /// Calcula o hash SHA-256 do conteudo documental para controlo de integridade.
        /// </summary>
        /// <param name="bytes">Conteudo binario do ficheiro.</param>
        /// <returns>Hash hexadecimal em maiusculas.</returns>
        private static string ComputeSha256(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
