using AutoMapper;
using TipMolde.Application.Dtos.UserDto;
using TipMolde.Domain.Entities;

namespace TipMolde.Application.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            ConfigureCreateMap();
            ConfigureUpdateMap();
            ConfigureChangeRoleMap();
            ConfigureResponseMap();
        }

        private void ConfigureCreateMap()
        {
            CreateMap<CreateUserDto, User>()
                .MapTrimmedRequired(dest => dest.Nome, src => src.Nome)
                .MapTrimmedRequired(dest => dest.Email, src => src.Email)
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.User_id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }

        private void ConfigureUpdateMap()
        {
            CreateMap<UpdateUserDto, User>()
                .MapTrimmedIfProvided(dest => dest.Nome, src => src.Nome)
                .MapTrimmedIfProvided(dest => dest.Email, src => src.Email)
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.User_id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }

        private void ConfigureChangeRoleMap()
        {
            CreateMap<ChangeUserRoleDto, User>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.User_id, opt => opt.Ignore())
                .ForMember(dest => dest.Nome, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }

        private void ConfigureResponseMap()
        {
            CreateMap<User, ResponseUserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        }
    }
}
