using AutoMapper;
using System.Globalization;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Mappings
{
    /// <summary>
    /// Profile AutoMapper dedicado ao agregado Maquina.
    /// </summary>
    /// <remarks>
    /// Centraliza o mapping entre DTOs e entidade para evitar logica dispersa no controller
    /// e reduzir divergencias no contrato HTTP.
    /// </remarks>
    public class MaquinaProfile : Profile
    {
        /// <summary>
        /// Configura os mapeamentos da feature Maquina.
        /// </summary>
        public MaquinaProfile()
        {
            ConfigureCreateMap();
            ConfigureUpdateMap();
            ConfigureResponseMap();
        }

        private void ConfigureCreateMap()
        {
            CreateMap<CreateMaquinaDto, Maquina>()
                .ForMember(dest => dest.Maquina_id, opt => opt.MapFrom(src => src.Maquina_id))
                .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero))
                .MapTrimmedRequired(dest => dest.NomeModelo, src => src.NomeModelo)
                .MapTrimmedOptional(dest => dest.IpAddress, src => src.IpAddress)
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado))
                .ForMember(dest => dest.FaseDedicada_id, opt => opt.MapFrom(src => src.FaseDedicada_id))
                .ForMember(dest => dest.FaseDedicada, opt => opt.Ignore());
        }

        private void ConfigureUpdateMap()
        {
            CreateMap<UpdateMaquinaDto, Maquina>()
                .MapValueIfProvided(dest => dest.Numero, src => src.Numero)
                .MapTrimmedIfProvided(dest => dest.NomeModelo, src => src.NomeModelo)
                .MapTrimmedIfProvided(dest => dest.IpAddress, src => src.IpAddress)
                .MapValueIfProvided(dest => dest.Estado, src => src.Estado)
                .MapValueIfProvided(dest => dest.FaseDedicada_id, src => src.FaseDedicada_id)
                .ForMember(dest => dest.Maquina_id, opt => opt.Ignore())
                .ForMember(dest => dest.FaseDedicada, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<Maquina, ResponseMaquinaDto>()
                .ForMember(dest => dest.FaseDedicadaNome, opt => opt.MapFrom(src => src.FaseDedicada == null ? string.Empty : FormatNomeFase(src.FaseDedicada.Nome)));
        }

        private static string FormatNomeFase(Enum? nome)
        {
            if (nome is null)
                return string.Empty;

            var text = nome.ToString().ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
        }
    }
}

