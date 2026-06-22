using TipMolde.Application.Dtos.RelatorioDto;
using TipMolde.Application.DTOs.RelatorioDto.Linhas;
using TipMolde.Application.Interface;

namespace TipMolde.Application.Interface.Relatorios
{
    /// <summary>
    /// Define queries especializadas para relatorios e indicadores.
    /// </summary>
    /// <remarks>
    /// O repositorio devolve read-models dedicados para evitar acoplamento do formato
    /// do documento ao modelo EF Core usado na persistencia.
    /// </remarks>
    public interface IRelatorioRepository
    {
        /// <summary>
        /// Obtem os dados agregados do ciclo de vida de um molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>Read-model do relatorio ou nulo quando o molde nao existe.</returns>
        Task<MoldeCicloVidaRelatorioDto?> ObterMoldeCicloVidaAsync(int moldeId);

        /// <summary>
        /// Obtem o contexto base usado na geracao da ficha FLT.
        /// </summary>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada para gerar a FLT.</param>
        /// <returns>Read-model base da FLT ou nulo quando o contexto nao existe.</returns>
        Task<FichaRelatorioBaseDto?> ObterFltRelatorioBaseAsync(int encomendaMoldeId);

        /// <summary>
        /// Obtem o contexto base usado pelas fichas editaveis exportadas.
        /// </summary>
        /// <param name="fichaId">Identificador interno da ficha editavel.</param>
        /// <returns>Read-model base da ficha ou nulo quando a ficha nao existe.</returns>
        Task<FichaRelatorioBaseDto?> ObterFichaRelatorioBaseAsync(int fichaId);

        Task<FichaFrmRelatorioDto?> ObterFichaFrmRelatorioAsync(int fichaId);
        Task<FichaFraRelatorioDto?> ObterFichaFraRelatorioAsync(int fichaId);
        Task<FichaFopRelatorioDto?> ObterFichaFopRelatorioAsync(int fichaId);
        Task<PagedResult<FichaFopGeralRelatorioLinhaDto>> ObterFopGeralAsync(DateTime dataInicio, DateTime dataFim, int page, int pageSize);
    }
}
