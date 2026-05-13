using AutoMapper;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Domain.Entities.Desenho;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado RegistoTempoProjeto.
    /// </summary>
    /// <remarks>
    /// Centraliza o mapping entre Dtos e entidade de dominio para evitar
    /// transformacoes dispersas no controller e no service.
    /// </remarks>
    public class RegistoTempoProjetoProfile : Profile
    {
        /// <summary>
        /// Configura os mapeamentos da feature RegistoTempoProjeto.
        /// </summary>
        public RegistoTempoProjetoProfile()
        {
            ConfigureCreateMap();
            ConfigureResponseMap();
        }

        private void ConfigureCreateMap()
        {
            CreateMap<CreateRegistoTempoProjetoDto, RegistoTempoProjeto>()
                .ForMember(dest => dest.Registo_Tempo_Projeto_id, opt => opt.Ignore())
                .ForMember(dest => dest.Estado_tempo, opt => opt.MapFrom(src => src.Estado_tempo!.Value))
                .ForMember(dest => dest.Data_hora, opt => opt.Ignore())
                .ForMember(dest => dest.Projeto, opt => opt.Ignore())
                .ForMember(dest => dest.Autor, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<RegistoTempoProjeto, ResponseRegistoTempoProjetoDto>();
        }
    }
}
