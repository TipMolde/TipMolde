using ClosedXML.Excel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TipMolde.Application.Dtos.RelatorioDto;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Application.Interface.Relatorios;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure.Service
{
    /// <summary>
    /// Gera artefactos documentais e KPI do modulo de relatorios.
    /// </summary>
    /// <remarks>
    /// Este servico orquestra templates Excel, PDF e persistencia documental.
    /// A regra de rastreabilidade obriga que o nome devolvido ao cliente seja exatamente o
    /// mesmo que fica versionado em FichaDocumento.
    /// </remarks>
    public class RelatorioService : IRelatorioService
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string DateFormat = "dd/MM/yyyy";

        private readonly IRelatorioRepository _relatorioRepository;
        private readonly IFichaDocumentoService _fichaDocumentoService;
        private readonly TemplateOptions _templateOptions;
        private readonly StorageOptions _storageOptions;
        private readonly IHostEnvironment _environment;

        /// <summary>
        /// Construtor de RelatorioService.
        /// </summary>
        /// <param name="relatorioRepository">Repositorio de leitura especializado para relatorios e indicadores.</param>
        /// <param name="templateOptions">Configuracao dos templates documentais.</param>
        /// <param name="storageOptions">Configuracao dos roots de storage e uploads.</param>
        /// <param name="environment">Ambiente da aplicacao usado para resolver paths relativos.</param>
        /// <param name="fichaDocumentoService">Servico responsavel por versionar e persistir os ficheiros gerados.</param>
        public RelatorioService(
            IRelatorioRepository relatorioRepository,
            IFichaDocumentoService fichaDocumentoService,
            IOptions<TemplateOptions> templateOptions,
            IOptions<StorageOptions> storageOptions,
            IHostEnvironment environment)
        {
            _relatorioRepository = relatorioRepository;
            _fichaDocumentoService = fichaDocumentoService;
            _templateOptions = templateOptions.Value;
            _storageOptions = storageOptions.Value;
            _environment = environment;
        }

        /// <summary>
        /// Gera o relatorio PDF do ciclo de vida completo de um molde.
        /// </summary>
        /// <remarks>
        /// Agrega informacao comercial, de desenho e de producao para cumprir o requisito RF-RL-01.
        /// Se alguma fonte transversal relevante nao existir, o relatorio deve sinalizar a falta de dados
        /// em vez de aparentar falsamente que o ciclo de vida esta completo.
        /// </remarks>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>Conteudo binario do PDF e nome do ficheiro gerado.</returns>
        public async Task<(byte[] Content, string FileName)> GerarCicloVidaMoldePdfAsync(int moldeId)
        {
            var relatorio = await _relatorioRepository.ObterMoldeCicloVidaAsync(moldeId)
                ?? throw new KeyNotFoundException($"Molde {moldeId} nao encontrado.");

            QuestPDF.Settings.License = LicenseType.Community;

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);

                    page.Header()
                        .Text($"Relatorio do Ciclo de Vida - Molde {relatorio.NumeroMolde}")
                        .SemiBold()
                        .FontSize(18);

                    page.Content().Column(column =>
                    {
                        column.Spacing(6);

                        AddSection(column, "Identificacao", [
                            ("Numero interno", relatorio.NumeroMolde),
                            ("Numero cliente", relatorio.NumeroMoldeCliente),
                            ("Nome", relatorio.NomeMolde),
                            ("Descricao", relatorio.DescricaoMolde),
                            ("Tipo de pedido", relatorio.TipoPedido.ToString()),
                            ("Numero de cavidades", relatorio.NumeroCavidades.ToString())
                        ]);

                        AddSection(column, "Comercial", [
                            ("Cliente", relatorio.ClienteNome),
                            ("Encomenda cliente", relatorio.NumeroEncomendaCliente),
                            ("Projeto cliente", relatorio.NumeroProjetoCliente),
                            ("Responsavel cliente", relatorio.NomeResponsavelCliente),
                            ("Data de registo da encomenda", FormatDate(relatorio.DataRegistoEncomenda)),
                            ("Data de entrega prevista", FormatDate(relatorio.DataEntregaPrevista))
                        ]);

                        AddSection(column, "Desenho", [
                            ("Projetos registados", relatorio.TotalProjetos.ToString()),
                            ("Revisoes registadas", relatorio.TotalRevisoes.ToString()),
                            ("Ultima revisao", FormatDate(relatorio.UltimaRevisaoEm))
                        ]);

                        AddSection(column, "Producao", [
                            ("Total de pecas", relatorio.TotalPecas.ToString()),
                            ("Material pendente", relatorio.MaterialPendente.ToString()),
                            ("Pecas ativas em maquinacao", relatorio.Maquinacao.ToString()),
                            ("Pecas ativas em erosao", relatorio.Erosao.ToString()),
                            ("Pecas em montagem", relatorio.Montagem.ToString()),
                            ("Pecas em espera", relatorio.EmEspera.ToString()),
                            ("Registos em trabalho", relatorio.EmTrabalho.ToString()),
                            ("Pecas concluidas", relatorio.Concluidas.ToString()),
                            ("Percentagem de conclusao", $"{relatorio.PercentagemConclusao:N2}%")
                        ]);
                    });
                });
            }).GeneratePdf();

            return (bytes, $"ciclo_vida_molde_{moldeId}.pdf");
        }

        /// <summary>
        /// Calcula os KPI do ciclo de vida produtivo de um molde.
        /// </summary>
        /// <remarks>
        /// Este metodo devolve uma vista resumida orientada ao dashboard sem gerar qualquer
        /// artefacto documental. A agregacao depende da mesma projection transversal usada no
        /// PDF para manter coerencia entre indicadores e detalhe do relatorio.
        /// </remarks>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>DTO com os principais indicadores agregados do molde.</returns>
        public async Task<MoldeCicloVidaDashboardDto> ObterDashboardMoldeAsync(int moldeId)
        {
            var relatorio = await _relatorioRepository.ObterMoldeCicloVidaAsync(moldeId)
                ?? throw new KeyNotFoundException($"Molde {moldeId} nao encontrado.");

            return new MoldeCicloVidaDashboardDto
            {
                MoldeId = relatorio.MoldeId,
                NumeroMolde = relatorio.NumeroMolde,
                TotalPecas = relatorio.TotalPecas,
                Maquinacao = relatorio.Maquinacao,
                Erosao = relatorio.Erosao,
                Montagem = relatorio.Montagem,
                EmEspera = relatorio.EmEspera,
                EmTrabalho = relatorio.EmTrabalho,
                Concluidas = relatorio.Concluidas,
                MaterialPendente = relatorio.MaterialPendente,
                PercentagemConclusao = relatorio.PercentagemConclusao
            };
        }

        /// <summary>
        /// Gera a ficha FLT oficial a partir do template configurado.
        /// </summary>
        /// <remarks>
        /// A FLT nao e uma ficha editavel persistida.
        /// O documento e gerado diretamente a partir da relacao Encomenda-Molde e por isso
        /// nao entra no fluxo de versionamento de FichaDocumento.
        /// </remarks>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada como contexto da FLT.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final do ficheiro gerado.</returns>
        public Task<(byte[] Content, string FileName)> GerarFichaExcelFLTAsync(int encomendaMoldeId, int userId)
        {
            return GerarFichaExcelSemVersionamentoAsync(
                encomendaMoldeId,
                _templateOptions.FichaFLT,
                _templateOptions.FolhaFLT,
                "FLT - TM.04.05",
                $"ficha_FLT_{encomendaMoldeId}.xlsx",
                _relatorioRepository.ObterFltRelatorioBaseAsync,
                FillFltBody);
        }

        /// <summary>
        /// Gera a ficha FRE oficial a partir do template configurado.
        /// </summary>
        /// <remarks>
        /// A FRE e preenchida com o estado atual da ficha de producao e fica sujeita ao fluxo
        /// normal de versionamento documental para garantir rastreabilidade da exportacao.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        public Task<(byte[] Content, string FileName)> GerarFichaExcelFREAsync(int fichaId, int userId)
        {
            return GerarFichaExcelAsync(
                fichaId,
                userId,
                _templateOptions.FichaFRE,
                _templateOptions.FolhaFRE,
                "FRE - TM.08.05",
                $"ficha_FRE_{fichaId}.xlsx",
                FillFreBody);
        }

        /// <summary>
        /// Gera a ficha FRM oficial a partir do template configurado.
        /// </summary>
        /// <remarks>
        /// A FRM e preenchida com as linhas de melhoria registadas no sistema e devolve
        /// o mesmo nome final versionado que fica persistido na trilha documental da ficha.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        public Task<(byte[] Content, string FileName)> GerarFichaExcelFRMAsync(int fichaId, int userId)
        {
            return GerarFichaExcelAsync(
                fichaId,
                userId,
                _relatorioRepository.ObterFichaFrmRelatorioAsync,
                _templateOptions.FichaFRM,
                _templateOptions.FolhaFRM,
                "FRM - TM.09.05",
                $"ficha_FRM_{fichaId}.xlsx",
                (ws, context) =>
                {
                    FillHeaderComum(ws, context.Base);
                    FillBlocoCliente(ws, context.Base);
                    FillFrmBody(ws, context);
                });
        }

        /// <summary>
        /// Gera a ficha FRA oficial a partir do template configurado.
        /// </summary>
        /// <remarks>
        /// A FRA incorpora as linhas de alteracao registadas para a ficha e persiste o ficheiro
        /// no historico documental para manter controlo de versoes e autoria da exportacao.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        public Task<(byte[] Content, string FileName)> GerarFichaExcelFRAAsync(int fichaId, int userId)
        {
            return GerarFichaExcelAsync(
                fichaId,
                userId,
                _relatorioRepository.ObterFichaFraRelatorioAsync,
                _templateOptions.FichaFRA,
                _templateOptions.FolhaFRA,
                "FRA - TM.010.05",
                $"ficha_FRA_{fichaId}.xlsx",
                (ws, context) =>
                {
                    FillHeaderComum(ws, context.Base);
                    FillBlocoCliente(ws, context.Base);
                    FillFraBody(ws, context);
                });
        }

        /// <summary>
        /// Gera a ficha FOP oficial a partir do template configurado.
        /// </summary>
        /// <remarks>
        /// A FOP materializa as ocorrencias operacionais registadas na ficha e devolve ao cliente
        /// o mesmo nome final que fica persistido na trilha documental auditavel.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        public Task<(byte[] Content, string FileName)> GerarFichaExcelFOPAsync(int fichaId, int userId)
        {
            return GerarFichaExcelAsync(
                fichaId,
                userId,
                _relatorioRepository.ObterFichaFopRelatorioAsync,
                _templateOptions.FichaFOP,
                _templateOptions.FolhaFOP,
                "FOP - TM.07.05",
                $"ficha_FOP_{fichaId}.xlsx",
                (ws, context) =>
                {
                    FillHeaderComum(ws, context.Base);
                    FillBlocoCliente(ws, context.Base);
                    FillFopBody(ws, context);
                });
        }

        /// <summary>
        /// Gera uma ficha Excel oficial nao versionada a partir de um template configurado.
        /// </summary>
        /// <remarks>
        /// Este helper e usado quando o contexto funcional nao corresponde a uma ficha editavel
        /// persistida, como acontece na FLT. O documento final e devolvido ao cliente sem criar
        /// registo em FichaDocumento.
        /// </remarks>
        /// <param name="contextId">Identificador do contexto funcional usado para obter os dados da ficha.</param>
        /// <param name="templateConfigKey">Nome do ficheiro template Excel a carregar.</param>
        /// <param name="folhaConfigKey">Nome configurado da folha a usar no template.</param>
        /// <param name="defaultSheetName">Nome por defeito da folha quando a configuracao nao existe.</param>
        /// <param name="fileName">Nome final do ficheiro devolvido ao cliente.</param>
        /// <param name="loadContext">Funcao responsavel por obter o read-model necessario ao preenchimento.</param>
        /// <param name="fillBody">Acao responsavel por preencher o corpo especifico da ficha.</param>
        /// <returns>Conteudo binario do Excel e nome final do ficheiro gerado.</returns>
        private async Task<(byte[] Content, string FileName)> GerarFichaExcelSemVersionamentoAsync(
            int contextId,
            string templateConfigKey,
            string folhaConfigKey,
            string defaultSheetName,
            string fileName,
            Func<int, Task<FichaRelatorioBaseDto?>> loadContext,
            Action<IXLWorksheet, FichaRelatorioBaseDto> fillBody)
        {
            var context = await loadContext(contextId)
                ?? throw new KeyNotFoundException($"Contexto {contextId} nao encontrado.");

            using var workbook = LoadWorkbook(templateConfigKey, folhaConfigKey, defaultSheetName, out var worksheet);
            FillHeaderComum(worksheet, context);
            fillBody(worksheet, context);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return (ms.ToArray(), fileName);
        }

        /// <summary>
        /// Gera uma ficha Excel oficial versionada a partir do read-model base da ficha.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Carrega o read-model base da ficha necessario ao preenchimento.
        /// 2. Valida a configuracao do template e da folha.
        /// 3. Preenche cabecalho e corpo especifico da ficha.
        /// 4. Persiste o artefacto gerado em FichaDocumento.
        /// 5. Devolve ao cliente o mesmo nome versionado que ficou auditado.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <param name="templateConfigKey">Nome do ficheiro template Excel a carregar.</param>
        /// <param name="folhaConfigKey">Nome configurado da folha a usar no template.</param>
        /// <param name="defaultSheetName">Nome por defeito da folha quando a configuracao nao existe.</param>
        /// <param name="fileName">Nome base do ficheiro a persistir.</param>
        /// <param name="fillBody">Acao responsavel por preencher o corpo especifico da ficha.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        private async Task<(byte[] Content, string FileName)> GerarFichaExcelAsync(
            int fichaId,
            int userId,
            string templateConfigKey,
            string folhaConfigKey,
            string defaultSheetName,
            string fileName,
            Action<IXLWorksheet, FichaRelatorioBaseDto> fillBody)
        {
            var context = await _relatorioRepository.ObterFichaRelatorioBaseAsync(fichaId)
                ?? throw new KeyNotFoundException($"Ficha {fichaId} nao encontrada.");

            using var workbook = LoadWorkbook(templateConfigKey, folhaConfigKey, defaultSheetName, out var worksheet);

            FillHeaderComum(worksheet, context);
            fillBody(worksheet, context);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var documento = await _fichaDocumentoService.GuardarGeradoAsync(
                fichaId,
                bytes,
                fileName,
                ExcelContentType,
                userId,
                "SISTEMA");

            return (bytes, documento.NomeFicheiro);
        }

        /// <summary>
        /// Gera uma ficha Excel oficial versionada a partir de um read-model especializado.
        /// </summary>
        /// <remarks>
        /// Este overload e usado quando a ficha exige uma projection propria com cabecalho e
        /// linhas funcionais especializadas, como acontece nas fichas FRM, FRA e FOP.
        /// </remarks>
        /// <typeparam name="TContext">Tipo do contexto especializado carregado para preencher a worksheet.</typeparam>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador responsavel pela geracao.</param>
        /// <param name="loadContext">Funcao responsavel por obter o contexto especializado da ficha.</param>
        /// <param name="templateFileName">Nome do ficheiro template Excel a carregar.</param>
        /// <param name="worksheetName">Nome configurado da folha a usar no template.</param>
        /// <param name="defaultSheetName">Nome por defeito da folha quando a configuracao nao existe.</param>
        /// <param name="fileName">Nome base do ficheiro a persistir.</param>
        /// <param name="fillWorksheet">Acao responsavel por preencher a worksheet com o contexto especializado.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        private async Task<(byte[] Content, string FileName)> GerarFichaExcelAsync<TContext>(
            int fichaId,
            int userId,
            Func<int, Task<TContext?>> loadContext,
            string templateFileName,
            string worksheetName,
            string defaultSheetName,
            string fileName,
            Action<IXLWorksheet, TContext> fillWorksheet)
        {
            var context = await loadContext(fichaId)
                ?? throw new KeyNotFoundException($"Ficha {fichaId} nao encontrada.");

            using var workbook = LoadWorkbook(templateFileName, worksheetName, defaultSheetName, out var worksheet);
            fillWorksheet(worksheet, context);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var documento = await _fichaDocumentoService.GuardarGeradoAsync(
                fichaId,
                bytes,
                fileName,
                ExcelContentType,
                userId,
                "SISTEMA");

            return (bytes, documento.NomeFicheiro);
        }

        /// <summary>
        /// Carrega o workbook e valida a configuracao do template Excel.
        /// </summary>
        /// <param name="templateFileName">Nome do ficheiro template Excel a carregar.</param>
        /// <param name="worksheetName">Nome configurado da folha a usar.</param>
        /// <param name="defaultSheetName">Nome por defeito da folha quando a configuracao nao existe.</param>
        /// <param name="worksheet">Folha carregada pronta a ser preenchida.</param>
        /// <returns>Workbook carregado a partir do template configurado.</returns>
        private XLWorkbook LoadWorkbook(
            string templateFileName,
            string worksheetName,
            string defaultSheetName,
            out IXLWorksheet worksheet)
        {
            var rootPath = ResolvePath(_templateOptions.RootPath);
            var templatePath = ResolveTemplatePath(rootPath, templateFileName);
            var finalWorksheetName = string.IsNullOrWhiteSpace(worksheetName) ? defaultSheetName : worksheetName;

            var workbook = new XLWorkbook(templatePath);
            worksheet = workbook.Worksheet(finalWorksheetName)
                ?? throw new KeyNotFoundException($"Folha '{finalWorksheetName}' nao encontrada no template.");

            return workbook;
        }

        /// <summary>
        /// Resolve o template Excel a usar e tolera a troca acidental entre FLT e FTL.
        /// </summary>
        /// <param name="rootPath">Pasta raiz onde os templates estao armazenados.</param>
        /// <param name="templateFileName">Nome configurado do template.</param>
        /// <returns>Caminho absoluto do primeiro template encontrado.</returns>
        private static string ResolveTemplatePath(string rootPath, string templateFileName)
        {
            var candidateFileNames = new List<string> { templateFileName };
            var upperTemplate = templateFileName.ToUpperInvariant();

            if (upperTemplate.Contains("FLT", StringComparison.Ordinal))
                candidateFileNames.Add(templateFileName.Replace("FLT", "FTL", StringComparison.OrdinalIgnoreCase));

            if (upperTemplate.Contains("FTL", StringComparison.Ordinal))
                candidateFileNames.Add(templateFileName.Replace("FTL", "FLT", StringComparison.OrdinalIgnoreCase));

            var attemptedPaths = new List<string>();

            foreach (var candidateFileName in candidateFileNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var candidatePath = Path.Combine(rootPath, candidateFileName);
                attemptedPaths.Add(candidatePath);

                if (File.Exists(candidatePath))
                    return candidatePath;
            }

            throw new FileNotFoundException(
                $"Template nao encontrado. Caminhos tentados: {string.Join(" | ", attemptedPaths)}");
        }

        /// <summary>
        /// Preenche o corpo funcional da ficha FLT com a informacao tecnica e comercial do molde.
        /// </summary>
        /// <remarks>
        /// O template FLT inclui imagem de capa, caracteristicas tecnicas, flags visuais e bloco
        /// cliente num layout proprio que nao reutiliza o bloco comum das restantes fichas.
        /// </remarks>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados necessarios ao preenchimento da FLT.</param>
        private void FillFltBody(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            var imagePath = ResolveImagePath(context.ImagemCapaPath);
            ws.AddPicture(imagePath)
                .MoveTo(ws.Range("B9:J24").FirstCell(), 5, 5)
                .WithSize(550, 320);

            ws.Cell("D28").Value = context.NumeroCavidades;
            ws.Cell("G28").Value = context.MaterialInjecao;
            ws.Cell("J28").Value = context.Contracao;
            ws.Cell("E29").Value = context.TipoInjecao;
            ws.Cell("J29").Value = context.AcabamentoPeca;
            ws.Cell("D30").Value = context.MaterialMacho;
            ws.Cell("D31").Value = context.MaterialCavidade;
            ws.Cell("D32").Value = context.MaterialMovimentos;
            ws.Cell("E34").Value = context.SistemaInjecao;

            SetX(ws, "F26", context.Cor == CorMolde.MONOCOLOR);
            SetX(ws, "H26", context.Cor == CorMolde.BICOLOR);
            SetX(ws, "J26", context.Cor == CorMolde.OUTRO);
            SetX(ws, "G33", context.LadoFixo);
            SetX(ws, "J33", context.LadoMovel);

            ws.Cell("E38").Value = context.TipoPedido.ToString();
            FillBlocoClienteFlt(ws, context);
            ws.Cell("B48").Value = context.DataEntregaPrevista.ToString(DateFormat);
        }

        /// <summary>
        /// Preenche o corpo funcional da ficha FRE com a informacao tecnica e comercial relevante.
        /// </summary>
        /// <remarks>
        /// A FRE usa um layout diferente da FLT, mas continua a depender da mesma base de dados
        /// tecnicos, cliente e imagem de capa do molde.
        /// </remarks>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados necessarios ao preenchimento da FRE.</param>
        private void FillFreBody(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            var imagePath = ResolveImagePath(context.ImagemCapaPath);
            ws.AddPicture(imagePath)
                .MoveTo(ws.Range("B9:J17").FirstCell(), 5, 5)
                .WithSize(550, 160);

            ws.Cell("D20").Value = context.NumeroCavidades;
            ws.Cell("G20").Value = context.MaterialInjecao;
            ws.Cell("J20").Value = context.Contracao;
            ws.Cell("E21").Value = context.TipoInjecao;
            ws.Cell("J21").Value = context.AcabamentoPeca;
            ws.Cell("E22").Value = context.SistemaInjecao;

            SetX(ws, "F18", context.Cor == CorMolde.MONOCOLOR);
            SetX(ws, "H18", context.Cor == CorMolde.BICOLOR);
            SetX(ws, "J18", context.Cor == CorMolde.OUTRO);

            FillBlocoClienteFre(ws, context);
        }

        /// <summary>
        /// Preenche o cabecalho comum das fichas oficiais.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados transversais do molde.</param>
        private static void FillHeaderComum(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            ws.Cell("I4").Value = DateTime.UtcNow.ToString(DateFormat);
            ws.Cell("C6").Value = context.MoldeNome;
            ws.Cell("J6").Value = context.MoldeNumero;
        }

        /// <summary>
        /// Preenche o bloco comum de dados do cliente usado nas fichas versionadas.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados comerciais do molde.</param>
        private static void FillBlocoCliente(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            ws.Cell("D9").Value = context.ClienteNome;
            ws.Cell("D10").Value = context.NomeServicoCliente;
            ws.Cell("E11").Value = context.NumeroProjetoCliente;
            ws.Cell("I11").Value = context.NumeroMoldeCliente;
            ws.Cell("E12").Value = context.NomeResponsavelCliente;
        }

        /// <summary>
        /// Preenche o bloco cliente especifico do layout FLT.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados comerciais do molde.</param>
        private static void FillBlocoClienteFlt(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            ws.Cell("D43").Value = context.ClienteNome;
            ws.Cell("D44").Value = context.NomeServicoCliente;
            ws.Cell("E45").Value = context.NumeroProjetoCliente;
            ws.Cell("I45").Value = context.NumeroMoldeCliente;
            ws.Cell("E46").Value = context.NomeResponsavelCliente;
        }

        /// <summary>
        /// Preenche o bloco cliente especifico do layout FRE.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Read-model base com os dados comerciais do molde.</param>
        private static void FillBlocoClienteFre(IXLWorksheet ws, FichaRelatorioBaseDto context)
        {
            ws.Cell("D26").Value = context.ClienteNome;
            ws.Cell("D27").Value = context.NomeServicoCliente;
            ws.Cell("E28").Value = context.NumeroProjetoCliente;
            ws.Cell("I28").Value = context.NumeroMoldeCliente;
            ws.Cell("E29").Value = context.NomeResponsavelCliente;
        }

        /// <summary>
        /// Preenche o corpo funcional da ficha FRM com as linhas de melhoria registadas.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Contexto de relatorio contendo cabecalho e linhas FRM.</param>
        private static void FillFrmBody(IXLWorksheet ws, FichaFrmRelatorioDto context)
        {
            const int startRow = 14;

            for (var i = 0; i < context.Linhas.Count; i++)
            {
                var row = startRow + (i * 2);
                var linha = context.Linhas[i];

                ws.Cell($"B{row}").Value = linha.Data.ToString(DateFormat);
                ws.Cell($"C{row}").Value = linha.Defeito;
                ws.Cell($"F{row}").Value = linha.Pormenor;
                ws.Cell($"I{row}").Value = linha.Verificado ? "Sim" : "Nao";
                ws.Cell($"J{row}").Value = linha.ResponsavelNome;
            }
        }

        /// <summary>
        /// Preenche o corpo funcional da ficha FRA com as linhas de alteracao registadas.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Contexto de relatorio contendo cabecalho e linhas FRA.</param>
        private static void FillFraBody(IXLWorksheet ws, FichaFraRelatorioDto context)
        {
            const int startRow = 14;

            for (var i = 0; i < context.Linhas.Count; i++)
            {
                var row = startRow + (i * 2);
                var linha = context.Linhas[i];

                ws.Cell($"B{row}").Value = linha.Data.ToString(DateFormat);
                ws.Cell($"C{row}").Value = linha.Alteracoes;
                ws.Cell($"I{row}").Value = linha.Verificado ? "Sim" : "Nao";
                ws.Cell($"J{row}").Value = linha.ResponsavelNome;
            }
        }

        /// <summary>
        /// Preenche o corpo funcional da ficha FOP com as ocorrencias operacionais registadas.
        /// </summary>
        /// <param name="ws">Worksheet do template oficial a preencher.</param>
        /// <param name="context">Contexto de relatorio contendo cabecalho e linhas FOP.</param>
        private static void FillFopBody(IXLWorksheet ws, FichaFopRelatorioDto context)
        {
            const int startRow = 14;

            for (var i = 0; i < context.Linhas.Count; i++)
            {
                var row = startRow + (i * 2);
                var linha = context.Linhas[i];

                ws.Cell($"B{row}").Value = linha.Data.ToString(DateFormat);
                ws.Cell($"C{row}").Value = linha.Ocorrencia;
                ws.Cell($"G{row}").Value = linha.Correcao;
                ws.Cell($"J{row}").Value = linha.ResponsavelNome;
            }
        }

        /// <summary>
        /// Resolve o caminho fisico da imagem de capa do molde.
        /// </summary>
        /// <remarks>
        /// Porque: os templates Excel dependem de um ficheiro fisico valido.
        /// Risco: se o path relativo for resolvido contra uma raiz errada, a exportacao falha
        /// ou incorpora uma imagem incorreta no documento oficial.
        /// </remarks>
        /// <param name="imagePathRaw">Caminho absoluto ou relativo guardado no sistema.</param>
        /// <returns>Caminho fisico absoluto pronto a ser consumido pelo ClosedXML.</returns>
        private string ResolveImagePath(string? imagePathRaw)
        {
            var attemptedPaths = new List<string>();

            foreach (var candidatePath in GetImageCandidates(imagePathRaw))
            {
                attemptedPaths.Add(candidatePath);

                if (!File.Exists(candidatePath))
                    continue;

                var ext = Path.GetExtension(candidatePath).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                    throw new InvalidOperationException("Formato de imagem nao suportado.");

                return candidatePath;
            }

            throw new FileNotFoundException(
                $"Imagem nao encontrada. Caminhos tentados: {string.Join(" | ", attemptedPaths)}");
        }

        private IEnumerable<string> GetImageCandidates(string? imagePathRaw)
        {
            if (!string.IsNullOrWhiteSpace(imagePathRaw))
            {
                var imagePath = imagePathRaw.Trim().Replace('\\', '/');

                if (Path.IsPathRooted(imagePath))
                {
                    yield return imagePath;
                }
                else
                {
                    yield return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, imagePath));
                    yield return Path.Combine(ResolvePath(_storageOptions.UploadsRootPath), imagePath.TrimStart('\\', '/'));
                }
            }

            yield return Path.Combine(ResolvePath(_templateOptions.RootPath), "image.png");
        }

        /// <summary>
        /// Resolve um path tecnico configurado para caminho absoluto no ambiente atual.
        /// </summary>
        /// <remarks>
        /// O metodo aceita paths absolutos ou relativos ao `ContentRootPath` da aplicacao para
        /// manter portabilidade entre desenvolvimento, testes e deploy.
        /// </remarks>
        /// <param name="configuredPath">Path tecnico vindo da configuracao da aplicacao.</param>
        /// <returns>Caminho absoluto pronto a ser usado pelo sistema de ficheiros.</returns>
        private string ResolvePath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Path tecnico nao configurado.");

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));
        }

        /// <summary>
        /// Escreve uma marcacao visual "X" numa celula quando a condicao e verdadeira.
        /// </summary>
        /// <param name="ws">Worksheet onde a marcacao sera aplicada.</param>
        /// <param name="cell">Referencia da celula a preencher.</param>
        /// <param name="condition">Condicao funcional que determina se a marcacao deve existir.</param>
        private static void SetX(IXLWorksheet ws, string cell, bool condition)
        {
            ws.Cell(cell).Value = condition ? "X" : string.Empty;
        }

        /// <summary>
        /// Adiciona uma secao textual ao PDF do ciclo de vida com rotulo e valor normalizado.
        /// </summary>
        /// <param name="column">Coluna QuestPDF onde a secao sera desenhada.</param>
        /// <param name="title">Titulo funcional da secao no relatorio.</param>
        /// <param name="rows">Linhas label/valor a apresentar ao utilizador final.</param>
        private static void AddSection(ColumnDescriptor column, string title, IEnumerable<(string Label, string? Value)> rows)
        {
            column.Item().PaddingTop(8).Text(title).Bold().FontSize(14);

            foreach (var row in rows)
                column.Item().Text($"{row.Label}: {Normalize(row.Value)}");
        }

        /// <summary>
        /// Formata uma data opcional para o padrao documental usado nos relatorios.
        /// </summary>
        /// <param name="value">Data a formatar.</param>
        /// <returns>Data no formato `dd/MM/yyyy` ou `n/d` quando nao existe valor.</returns>
        private static string FormatDate(DateTime? value)
        {
            return value?.ToString(DateFormat) ?? "n/d";
        }

        /// <summary>
        /// Normaliza um texto opcional para apresentacao documental.
        /// </summary>
        /// <param name="value">Valor textual a apresentar.</param>
        /// <returns>Texto sem espacos exteriores ou `n/d` quando o valor nao existe.</returns>
        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/d" : value.Trim();
        }
    }
}
