using AutoMapper;
using TipMolde.Application.Dtos.PecaDto;
using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado Peca.
    /// </summary>
    /// <remarks>
    /// Centraliza o mapping entre Dtos e entidade para evitar logica dispersa no controller
    /// e reduzir divergencias no contrato HTTP.
    /// </remarks>
    public class PecaProfile : Profile
    {
        /// <summary>
        /// Configura os mapeamentos da feature Peca.
        /// </summary>
        public PecaProfile()
        {
            ConfigureCreateMap();
            ConfigureUpdateMap();
            ConfigureResponseMap();
        }

        private void ConfigureCreateMap()
        {
            CreateMap<CreatePecaDto, Peca>()
                .ForMember(dest => dest.Peca_id, opt => opt.Ignore())
                .MapTrimmedOptional(dest => dest.NumeroPeca, src => src.NumeroPeca)
                .MapTrimmedRequired(dest => dest.Designacao, src => src.Designacao)
                .ForMember(dest => dest.Prioridade, opt => opt.MapFrom(src => src.Prioridade))
                .ForMember(dest => dest.Quantidade, opt => opt.MapFrom(src => src.Quantidade))
                .MapTrimmedOptional(dest => dest.Referencia, src => src.Referencia)
                .MapTrimmedOptional(dest => dest.MaterialDesignacao, src => src.MaterialDesignacao)
                .MapTrimmedOptional(dest => dest.TratamentoTermico, src => src.TratamentoTermico)
                .MapTrimmedOptional(dest => dest.Massa, src => src.Massa)
                .MapTrimmedOptional(dest => dest.Observacao, src => src.Observacao)
                .ForMember(dest => dest.MaterialRecebido, opt => opt.MapFrom(src => src.MaterialRecebido))
                .ForMember(dest => dest.ProximaFase_id, opt => opt.MapFrom(src => src.ProximaFase_id))
                .ForMember(dest => dest.ProximaFase, opt => opt.Ignore())
                .ForMember(dest => dest.Molde_id, opt => opt.MapFrom(src => src.Molde_id))
                .ForMember(dest => dest.Molde, opt => opt.Ignore());
        }

        private void ConfigureUpdateMap()
        {
            CreateMap<UpdatePecaDto, Peca>()
                .MapTrimmedIfProvided(dest => dest.NumeroPeca, src => src.NumeroPeca)
                .MapTrimmedIfProvided(dest => dest.Designacao, src => src.Designacao)
                .MapValueIfProvided(dest => dest.Prioridade, src => src.Prioridade)
                .MapValueIfProvided(dest => dest.Quantidade, src => src.Quantidade)
                .MapTrimmedIfProvided(dest => dest.Referencia, src => src.Referencia)
                .MapTrimmedIfProvided(dest => dest.MaterialDesignacao, src => src.MaterialDesignacao)
                .MapTrimmedIfProvided(dest => dest.TratamentoTermico, src => src.TratamentoTermico)
                .MapTrimmedIfProvided(dest => dest.Massa, src => src.Massa)
                .MapTrimmedIfProvided(dest => dest.Observacao, src => src.Observacao)
                .MapValueIfProvided(dest => dest.MaterialRecebido, src => src.MaterialRecebido)
                .MapValueIfProvided(dest => dest.ProximaFase_id, src => src.ProximaFase_id)
                .ForMember(dest => dest.Peca_id, opt => opt.Ignore())
                .ForMember(dest => dest.Molde_id, opt => opt.Ignore())
                .ForMember(dest => dest.ProximaFase, opt => opt.Ignore())
                .ForMember(dest => dest.Molde, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<Peca, ResponsePecaDto>()
                .ForMember(dest => dest.PecaId, opt => opt.MapFrom(src => src.Peca_id))
                .ForMember(dest => dest.ProximaFaseNome, opt => opt.MapFrom(src => src.ProximaFase == null ? null : src.ProximaFase.Nome.ToString()))
                .ForMember(dest => dest.Molde_id, opt => opt.MapFrom(src => src.Molde_id));
        }
    }
}
