using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Service;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Tests.Unitario.Infrastructure
{
    [TestFixture]
    [Category("Unit")]
    public sealed class JwtTokenServiceTests
    {
        [Test(Description = "TJWT001 - CreateToken deve emitir JWT com claims, issuer, audience e expiracao configurados.")]
        public void CreateToken_Should_ReturnJwt_WithConfiguredClaimsAndLifetime()
        {
            // ARRANGE
            var options = Options.Create(new JwtOptions
            {
                Issuer = "tipmolde-api",
                Audience = "tipmolde-web",
                SecretKey = "uma-chave-super-secreta-com-tamanho-suficiente-12345",
                ExpirationMinutes = 30
            });

            var sut = new JwtTokenService(options);
            var user = new User
            {
                User_id = 17,
                Nome = "Utilizador JWT",
                Email = "jwt@tipmolde.pt",
                Password = "hash",
                Role = UserRole.ADMIN
            };

            var before = DateTime.UtcNow;

            // ACT
            var token = sut.CreateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var after = DateTime.UtcNow;

            // ASSERT
            jwt.Issuer.Should().Be("tipmolde-api");
            jwt.Audiences.Should().ContainSingle().Which.Should().Be("tipmolde-web");
            jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == "17");
            jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Email && x.Value == "jwt@tipmolde.pt");
            jwt.Claims.Should().Contain(x => x.Type == ClaimTypes.Email && x.Value == "jwt@tipmolde.pt");
            jwt.Claims.Should().Contain(x => x.Type == ClaimTypes.Role && x.Value == UserRole.ADMIN.ToString());
            jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(x.Value));
            jwt.ValidTo.Should().BeOnOrAfter(before.AddMinutes(29));
            jwt.ValidTo.Should().BeOnOrBefore(after.AddMinutes(31));
        }
    }
}
