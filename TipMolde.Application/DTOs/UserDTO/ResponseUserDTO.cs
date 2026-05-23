using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.UserDto
{
    public class ResponseUserDto
    {
        public int User_id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; }
    }
}
