using TipMolde.Application.Dtos.RelatorioDto;
using TipMolde.Application.DTOs.RelatorioDto.Linhas;
using TipMolde.Application.Interface;

namespace TipMolde.Application.Interface.Relatorios
{
    /// <summary>
    /// Define os casos de uso de relatorios e indicadores do modulo de moldes e fichas.
    /// </summary>
    /// <remarks>
    /// O contrato separa a geracao documental dos indicadores operacionais para manter
    /// fronteiras claras entre exportacao de artefactos e consumo de dados pelo frontend.
    /// </remarks>
    public interface IRelatorioService
    {
        /// <summary>
        /// Gera o relatorio PDF do ciclo de vida completo de um molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>Conteudo binario do PDF e respetivo nome de ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarCicloVidaMoldePdfAsync(int moldeId);

        /// <summary>
        /// Calcula os KPI do ciclo de vida produtivo de um molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>DTO com indicadores agregados do molde.</returns>
        Task<MoldeCicloVidaDashboardDto> ObterDashboardMoldeAsync(int moldeId);

        /// <summary>
        /// Gera a ficha FLT pre-preenchida com os dados registados no sistema.
        /// </summary>
        /// <remarks>
        /// A FLT nao depende de uma ficha editavel persistida.
        /// O documento e gerado diretamente a partir da relacao Encomenda-Molde.
        /// </remarks>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada como contexto da FLT.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFichaExcelFLTAsync(int encomendaMoldeId, int userId);

        /// <summary>
        /// Gera a ficha FRE pre-preenchida com os dados registados no sistema.
        /// </summary>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFichaExcelFREAsync(int fichaId, int userId);

        /// <summary>
        /// Gera a ficha FRM pre-preenchida com os dados registados no sistema.
        /// </summary>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFichaExcelFRMAsync(int fichaId, int userId);

        /// <summary>
        /// Gera a ficha FRA pre-preenchida com os dados registados no sistema.
        /// </summary>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFichaExcelFRAAsync(int fichaId, int userId);

        /// <summary>
        /// Gera a ficha FOP pre-preenchida com os dados registados no sistema.
        /// </summary>
        /// <param name="fichaId">Identificador interno da ficha de producao.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final versionado do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFichaExcelFOPAsync(int fichaId, int userId);

        /// <summary>
        /// Gera a FOP geral em Excel para um intervalo de datas.
        /// </summary>
        /// <param name="dataInicio">Data inicial do intervalo.</param>
        /// <param name="dataFim">Data final do intervalo.</param>
        /// <param name="userId">Identificador do utilizador que desencadeou a geracao.</param>
        /// <returns>Conteudo binario do Excel e nome final do ficheiro.</returns>
        Task<(byte[] Content, string FileName)> GerarFopGeralExcelAsync(DateTime dataInicio, DateTime dataFim, int userId);

        /// <summary>
        /// Lista as ocorrencias gerais da FOP por intervalo de datas.
        /// </summary>
        /// <param name="dataInicio">Data inicial do intervalo.</param>
        /// <param name="dataFim">Data final do intervalo.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com as linhas da FOP geral.</returns>
        Task<PagedResult<FichaFopGeralRelatorioLinhaDto>> ObterFopGeralAsync(DateTime dataInicio, DateTime dataFim, int page = 1, int pageSize = 10);
    }
}
