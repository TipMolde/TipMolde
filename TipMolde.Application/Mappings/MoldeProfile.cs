using AutoMapper;
using TipMolde.Application.Dtos.MoldeDto;
using TipMolde.Domain.Entities.Producao;
using static TipMolde.Application.Mappings.MappingProfileExtensions;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado Molde.
    /// </summary>
    /// <remarks>
    /// Centraliza o mapping entre Dtos e entidades para evitar logica dispersa no controller.
    /// </remarks>
    public class MoldeProfile : Profile
    {
        /// <summary>
        /// Configura os mapeamentos da feature Molde.
        /// </summary>
        public MoldeProfile()
        {
            ConfigureMoldeCreateMap();
            ConfigureEspecificacoesCreateMap();
            ConfigureMoldeUpdateMap();
            ConfigureEspecificacoesUpdateMap();
            ConfigureResponseMap();
        }

        private void ConfigureMoldeCreateMap()
        {
            CreateMap<CreateMoldeDto, Molde>()
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .MapTrimmedRequired(dest => dest.Numero, src => src.Numero)
                .MapTrimmedOptional(dest => dest.NumeroMoldeCliente, src => src.NumeroMoldeCliente)
                .MapTrimmedOptional(dest => dest.Nome, src => src.Nome)
                .MapTrimmedOptional(dest => dest.ImagemCapaPath, src => src.ImagemCapaPath)
                .MapTrimmedOptional(dest => dest.Descricao, src => src.Descricao)
                .ForMember(dest => dest.Especificacoes, opt => opt.Ignore())
                .ForMember(dest => dest.Pecas, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendasMoldes, opt => opt.Ignore());
        }

        private void ConfigureEspecificacoesCreateMap()
        {
            CreateMap<CreateMoldeDto, EspecificacoesTecnicas>()
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore())
                .MapTrimmedOptional(dest => dest.TipoInjecao, src => src.TipoInjecao)
                .MapTrimmedOptional(dest => dest.SistemaInjecao, src => src.SistemaInjecao)
                .MapTrimmedOptional(dest => dest.AcabamentoPeca, src => src.AcabamentoPeca)
                .MapTrimmedOptional(dest => dest.MaterialMacho, src => src.MaterialMacho)
                .MapTrimmedOptional(dest => dest.MaterialCavidade, src => src.MaterialCavidade)
                .MapTrimmedOptional(dest => dest.MaterialMovimentos, src => src.MaterialMovimentos)
                .MapTrimmedOptional(dest => dest.MaterialInjecao, src => src.MaterialInjecao)
                .ForMember(dest => dest.LadoFixo, opt => opt.Ignore())
                .ForMember(dest => dest.LadoMovel, opt => opt.Ignore());
        }

        private void ConfigureMoldeUpdateMap()
        {
            CreateMap<UpdateMoldeDto, Molde>()
                .MapTrimmedIfProvided(dest => dest.Numero, src => src.Numero)
                .MapTrimmedIfProvided(dest => dest.NumeroMoldeCliente, src => src.NumeroMoldeCliente)
                .MapTrimmedIfProvided(dest => dest.Nome, src => src.Nome)
                .MapTrimmedIfProvided(dest => dest.ImagemCapaPath, src => src.ImagemCapaPath)
                .MapTrimmedIfProvided(dest => dest.Descricao, src => src.Descricao)
                .MapValueIfProvided(dest => dest.Numero_cavidades, src => src.Numero_cavidades)
                .MapValueIfProvided(dest => dest.TipoPedido, src => src.TipoPedido)
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Especificacoes, opt => opt.Ignore())
                .ForMember(dest => dest.Pecas, opt => opt.Ignore())
                .ForMember(dest => dest.EncomendasMoldes, opt => opt.Ignore());
        }

        private void ConfigureEspecificacoesUpdateMap()
        {
            CreateMap<UpdateMoldeDto, EspecificacoesTecnicas>()
                .MapValueIfProvided(dest => dest.Largura, src => src.Largura)
                .MapValueIfProvided(dest => dest.Comprimento, src => src.Comprimento)
                .MapValueIfProvided(dest => dest.Altura, src => src.Altura)
                .MapValueIfProvided(dest => dest.PesoEstimado, src => src.PesoEstimado)
                .MapTrimmedIfProvided(dest => dest.TipoInjecao, src => src.TipoInjecao)
                .MapTrimmedIfProvided(dest => dest.SistemaInjecao, src => src.SistemaInjecao)
                .MapValueIfProvided(dest => dest.Contracao, src => src.Contracao)
                .MapTrimmedIfProvided(dest => dest.AcabamentoPeca, src => src.AcabamentoPeca)
                .MapValueIfProvided(dest => dest.Cor, src => src.Cor)
                .MapTrimmedIfProvided(dest => dest.MaterialMacho, src => src.MaterialMacho)
                .MapTrimmedIfProvided(dest => dest.MaterialCavidade, src => src.MaterialCavidade)
                .MapTrimmedIfProvided(dest => dest.MaterialMovimentos, src => src.MaterialMovimentos)
                .MapTrimmedIfProvided(dest => dest.MaterialInjecao, src => src.MaterialInjecao)
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore())
                .ForMember(dest => dest.LadoFixo, opt => opt.Ignore())
                .ForMember(dest => dest.LadoMovel, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<Molde, ResponseMoldeDto>()
                .ForMember(dest => dest.MoldeId, opt => opt.MapFrom(src => src.Molde_id))
                .ForMember(dest => dest.NumeroMoldeCliente, opt => opt.MapFrom(src => src.NumeroMoldeCliente))
                .ForMember(dest => dest.Largura, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.Largura)))
                .ForMember(dest => dest.Comprimento, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.Comprimento)))
                .ForMember(dest => dest.Altura, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.Altura)))
                .ForMember(dest => dest.PesoEstimado, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.PesoEstimado)))
                .ForMember(dest => dest.TipoInjecao, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.TipoInjecao)))
                .ForMember(dest => dest.SistemaInjecao, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.SistemaInjecao)))
                .ForMember(dest => dest.Contracao, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.Contracao)))
                .ForMember(dest => dest.AcabamentoPeca, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.AcabamentoPeca)))
                .ForMember(dest => dest.Cor, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.Cor)))
                .ForMember(dest => dest.MaterialMacho, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.MaterialMacho)))
                .ForMember(dest => dest.MaterialCavidade, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.MaterialCavidade)))
                .ForMember(dest => dest.MaterialMovimentos, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.MaterialMovimentos)))
                .ForMember(dest => dest.MaterialInjecao, opt => opt.MapFrom(src => GetOptionalValue(src.Especificacoes, e => e.MaterialInjecao)));
        }
    }
}
