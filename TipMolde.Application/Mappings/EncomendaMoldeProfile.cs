using AutoMapper;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Domain.Entities.Comercio;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado EncomendaMolde.
    /// </summary>
    public class EncomendaMoldeProfile : Profile
    {
        public EncomendaMoldeProfile()
        {
            ConfigureCreateMap();
            ConfigureUpdateMap();
            ConfigureResponseMap();
        }

        private void ConfigureCreateMap()
        {
            CreateMap<CreateEncomendaMoldeDto, EncomendaMolde>()
                .ForMember(dest => dest.EncomendaMolde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.Encomenda, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore())
                .ForMember(dest => dest.Fichas, opt => opt.Ignore());
        }

        private void ConfigureUpdateMap()
        {
            CreateMap<UpdateEncomendaMoldeDto, EncomendaMolde>()
                .MapValueIfProvided(dest => dest.Quantidade, src => src.Quantidade)
                .MapValueIfProvided(dest => dest.Prioridade, src => src.Prioridade)
                .MapValueIfProvided(dest => dest.DataEntregaPrevista, src => src.DataEntregaPrevista)
                .ForMember(dest => dest.EncomendaMolde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.Encomenda_id, opt => opt.Ignore())
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Encomenda, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore())
                .ForMember(dest => dest.Fichas, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<EncomendaMolde, ResponseEncomendaMoldeDto>()
                .ForMember(dest => dest.NumeroEncomendaCliente, opt => opt.MapFrom(src => MappingProfileExtensions.GetOptionalValue(src.Encomenda, encomenda => encomenda.NumeroEncomendaCliente)))
                .ForMember(dest => dest.NumeroMolde, opt => opt.MapFrom(src => MappingProfileExtensions.GetOptionalValue(src.Molde, molde => molde.Numero)))
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.Encomenda == null
                    ? null
                    : src.Encomenda.Cliente == null
                        ? null
                        : src.Encomenda.Cliente.Nome))
                .ForMember(dest => dest.NomeMolde, opt => opt.MapFrom(src => MappingProfileExtensions.GetOptionalValue(src.Molde, molde => molde.Nome)))
                .ForMember(dest => dest.DescricaoMolde, opt => opt.MapFrom(src => MappingProfileExtensions.GetOptionalValue(src.Molde, molde => molde.Descricao)))
                .ForMember(dest => dest.ImagemCapaPath, opt => opt.MapFrom(src => MappingProfileExtensions.GetOptionalValue(src.Molde, molde => molde.ImagemCapaPath)));
        }
    }
}
