using System.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Tests.Integracao;

[TestFixture]
[Category("Integration")]
public sealed class MySqlSmokeTests
{
    [Test(Description = "TMYSQL001 - A base MySQL de testes deve aceitar ligacao real da suite.")]
    public async Task CanConnectAsync_Should_ReturnTrue()
    {
        await using var context = MySqlIntegrationTestContextFactory.CreateContext();

        var canConnect = await context.Database.CanConnectAsync();

        canConnect.Should().BeTrue();
    }

    [Test(Description = "TMYSQL002 - Os enums mais criticos devem persistir como texto na base MySQL.")]
    public async Task EnumColumns_Should_BeStoredAsStrings()
    {
        await using var context = MySqlIntegrationTestContextFactory.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..10];
        var numericSuffix = Math.Abs(uniqueSuffix.GetHashCode()).ToString("D10")[..9];

        var user = new User
        {
            Nome = $"User {uniqueSuffix}",
            Email = $"user_{uniqueSuffix}@tipmolde.test",
            Password = "hashed-password",
            Role = UserRole.ADMIN
        };

        var fase = new FasesProducao
        {
            Nome = NomeFases.MAQUINACAO,
            Descricao = $"Fase {uniqueSuffix}"
        };

        var molde = new Molde
        {
            Numero = $"M-{uniqueSuffix}",
            Nome = $"Molde {uniqueSuffix}",
            NumeroMoldeCliente = $"CLI-{uniqueSuffix}",
            Descricao = "Teste de persistencia MySQL",
            Numero_cavidades = 1,
            TipoPedido = TipoPedido.REPARACAO
        };

        var especificacoes = new EspecificacoesTecnicas
        {
            Molde = molde,
            Cor = CorMolde.BICOLOR
        };

        var fornecedor = new Fornecedor
        {
            Nome = $"Fornecedor {uniqueSuffix}",
            NIF = numericSuffix,
            Email = $"fornecedor_{uniqueSuffix}@tipmolde.test"
        };

        var pedido = new PedidoMaterial
        {
            DataPedido = DateTime.UtcNow,
            Estado = EstadoPedido.RECEBIDO,
            Fornecedor = fornecedor
        };

        await context.AddRangeAsync(user, fase, molde, especificacoes, fornecedor, pedido);
        await context.SaveChangesAsync();

        (await ReadStoredStringAsync(context, user, nameof(User.Role))).Should().Be("ADMIN");
        (await ReadStoredStringAsync(context, fase, nameof(FasesProducao.Nome))).Should().Be("MAQUINACAO");
        (await ReadStoredStringAsync(context, molde, nameof(Molde.TipoPedido))).Should().Be("REPARACAO");
        (await ReadStoredStringAsync(context, especificacoes, nameof(EspecificacoesTecnicas.Cor))).Should().Be("BICOLOR");
        (await ReadStoredStringAsync(context, pedido, nameof(PedidoMaterial.Estado))).Should().Be("RECEBIDO");
    }

    private static async Task<string?> ReadStoredStringAsync<TEntity>(
        ApplicationDbContext context,
        TEntity entity,
        string propertyName)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entidade '{typeof(TEntity).Name}' nao mapeada no modelo EF.");

        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"Tabela da entidade '{typeof(TEntity).Name}' nao encontrada.");

        var schema = entityType.GetSchema();
        var tableIdentifier = StoreObjectIdentifier.Table(tableName, schema);

        var property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Propriedade '{propertyName}' nao encontrada na entidade '{typeof(TEntity).Name}'.");

        var columnName = property.GetColumnName(tableIdentifier)
            ?? throw new InvalidOperationException($"Coluna da propriedade '{propertyName}' nao encontrada.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Chave primaria da entidade '{typeof(TEntity).Name}' nao encontrada.");

        if (primaryKey.Properties.Count != 1)
        {
            throw new InvalidOperationException($"A entidade '{typeof(TEntity).Name}' nao tem chave primaria simples.");
        }

        var keyProperty = primaryKey.Properties[0];
        var keyColumnName = keyProperty.GetColumnName(tableIdentifier)
            ?? throw new InvalidOperationException($"Coluna da chave primaria de '{typeof(TEntity).Name}' nao encontrada.");

        var keyValue = context.Entry(entity).Property(keyProperty.Name).CurrentValue
            ?? throw new InvalidOperationException($"Valor da chave primaria de '{typeof(TEntity).Name}' nao encontrado.");

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT `{columnName}` FROM `{tableName}` WHERE `{keyColumnName}` = @id LIMIT 1";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = keyValue;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }
}
