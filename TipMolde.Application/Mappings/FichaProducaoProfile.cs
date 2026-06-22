using AutoMapper;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado FichaProducao.
    /// </summary>
    public class FichaProducaoProfile : Profile
    {
        public FichaProducaoProfile()
        {
            ConfigureFichaResponseMaps();
            ConfigureCreateFichaMap();
            ConfigureLinhaMaps();
        }

        private void ConfigureFichaResponseMaps()
        {
            CreateMap<FichaProducao, ResponseFichaProducaoDto>();

            CreateMap<FichaProducao, ResponseFichaProducaoDetalheDto>()
                .ForMember(dest => dest.NumeroMolde, opt => opt.MapFrom(src => src.EncomendaMolde != null && src.EncomendaMolde.Molde != null ? src.EncomendaMolde.Molde.Numero : null))
                .ForMember(dest => dest.NomeMolde, opt => opt.MapFrom(src => src.EncomendaMolde != null && src.EncomendaMolde.Molde != null ? src.EncomendaMolde.Molde.Nome : null))
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.EncomendaMolde != null && src.EncomendaMolde.Encomenda != null && src.EncomendaMolde.Encomenda.Cliente != null ? src.EncomendaMolde.Encomenda.Cliente.Nome : null))
                .ForMember(dest => dest.NumeroEncomendaCliente, opt => opt.MapFrom(src => src.EncomendaMolde != null && src.EncomendaMolde.Encomenda != null ? src.EncomendaMolde.Encomenda.NumeroEncomendaCliente : null))
                .ForMember(dest => dest.LinhasFrm, opt => opt.Ignore())
                .ForMember(dest => dest.LinhasFra, opt => opt.Ignore())
                .ForMember(dest => dest.LinhasFop, opt => opt.Ignore());
        }

        private void ConfigureCreateFichaMap()
        {
            CreateMap<CreateFichaProducaoDto, FichaProducao>()
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendaMolde, opt => opt.Ignore())
                .ForMember(dest => dest.Documentos, opt => opt.Ignore());
        }

        private void ConfigureLinhaMaps()
        {
            CreateMap<CreateFichaFrmLinhaDto, FichaFrmLinha>()
                .ForMember(dest => dest.FichaFrmLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFrmLinha, ResponseFichaFrmLinhaDto>()
                .ForMember(dest => dest.FichaFrm_id, opt => opt.MapFrom(src => src.FichaProducao_id));

            CreateMap<CreateFichaFraLinhaDto, FichaFraLinha>()
                .ForMember(dest => dest.FichaFraLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFraLinha, ResponseFichaFraLinhaDto>()
                .ForMember(dest => dest.FichaFra_id, opt => opt.MapFrom(src => src.FichaProducao_id));

            CreateMap<CreateFichaFopLinhaDto, FichaFopLinha>()
                .ForMember(dest => dest.FichaFopLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaProducao, opt => opt.Ignore())
                .ForMember(dest => dest.Peca, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFopLinha, ResponseFichaFopLinhaDto>()
                .ForMember(dest => dest.FichaFop_id, opt => opt.MapFrom(src => src.FichaProducao_id));
        }
    }
}
