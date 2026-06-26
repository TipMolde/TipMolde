using TipMolde.Application.Dtos.IndustrialMiddlewareDto;

namespace TipMolde.Application.Interface.Industrial
{
    /// <summary>
    /// Porta de saida da aplicacao para pedir testes tecnicos ao middleware industrial.
    /// </summary>
    public interface IIndustrialMiddlewareClient
    {
        Task<ProtocolDetectionResultDto> DetectProtocolAsync(string machineIp);
    }
}
