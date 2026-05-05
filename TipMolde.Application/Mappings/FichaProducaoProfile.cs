using AutoMapper;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas.Linhas;

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
            ConfigureCreateFichaMaps();
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

        private void ConfigureCreateFichaMaps()
        {
            ConfigureCreateFichaFreMap();
            ConfigureCreateFichaFrmMap();
            ConfigureCreateFichaFraMap();
            ConfigureCreateFichaFopMap();
        }

        private void ConfigureCreateFichaFreMap()
        {
            CreateMap<CreateFichaProducaoDto, FichaFre>()
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaEm, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.Ativa, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaEm, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendaMolde, opt => opt.Ignore())
                .ForMember(dest => dest.Relatorios, opt => opt.Ignore());
        }

        private void ConfigureCreateFichaFrmMap()
        {
            CreateMap<CreateFichaProducaoDto, FichaFrm>()
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaEm, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.Ativa, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaEm, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendaMolde, opt => opt.Ignore())
                .ForMember(dest => dest.Relatorios, opt => opt.Ignore())
                .ForMember(dest => dest.Linhas, opt => opt.Ignore());
        }

        private void ConfigureCreateFichaFraMap()
        {
            CreateMap<CreateFichaProducaoDto, FichaFra>()
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaEm, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.Ativa, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaEm, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendaMolde, opt => opt.Ignore())
                .ForMember(dest => dest.Relatorios, opt => opt.Ignore())
                .ForMember(dest => dest.Linhas, opt => opt.Ignore());
        }

        private void ConfigureCreateFichaFopMap()
        {
            CreateMap<CreateFichaProducaoDto, FichaFop>()
                .ForMember(dest => dest.FichaProducao_id, opt => opt.Ignore())
                .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaEm, opt => opt.Ignore())
                .ForMember(dest => dest.SubmetidaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.Ativa, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaEm, opt => opt.Ignore())
                .ForMember(dest => dest.DesativadaPor_user_id, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendaMolde, opt => opt.Ignore())
                .ForMember(dest => dest.Relatorios, opt => opt.Ignore())
                .ForMember(dest => dest.Linhas, opt => opt.Ignore());
        }

        private void ConfigureLinhaMaps()
        {
            CreateMap<CreateFichaFrmLinhaDto, FichaFrmLinha>()
                .ForMember(dest => dest.FichaFrmLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFrm_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFrm, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFrmLinha, ResponseFichaFrmLinhaDto>();

            CreateMap<CreateFichaFraLinhaDto, FichaFraLinha>()
                .ForMember(dest => dest.FichaFraLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFra_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFra, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFraLinha, ResponseFichaFraLinhaDto>();

            CreateMap<CreateFichaFopLinhaDto, FichaFopLinha>()
                .ForMember(dest => dest.FichaFopLinha_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFop_id, opt => opt.Ignore())
                .ForMember(dest => dest.FichaFop, opt => opt.Ignore())
                .ForMember(dest => dest.CriadoEm, opt => opt.Ignore());

            CreateMap<FichaFopLinha, ResponseFichaFopLinhaDto>();
        }
    }
}
