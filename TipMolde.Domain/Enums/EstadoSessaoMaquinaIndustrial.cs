namespace TipMolde.Domain.Enums
{
    /// <summary>
    /// Estado operacional de uma sessao industrial ativa ou historica.
    /// </summary>
    public enum EstadoSessaoMaquinaIndustrial
    {
        ATIVA,
        AGUARDAR_CONFIRMACAO_PARAGEM,
        FECHADA
    }
}
