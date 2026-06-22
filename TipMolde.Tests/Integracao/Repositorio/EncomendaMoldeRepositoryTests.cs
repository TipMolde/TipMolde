using FluentAssertions;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    /// <summary>
    /// Testes de integracao do repositorio de EncomendaMolde.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class EncomendaMoldeRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TENCMREP1 - GetByEncomendaId deve carregar molde associado.")]
        public async Task GetByEncomendaIdAsync_Should_LoadMolde_When_LinkExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-001" };
            var molde = new Molde { Numero = "M-001", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddAsync(new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = DateTime.UtcNow.AddDays(10)
            });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.GetByEncomendaIdAsync(encomenda.Encomenda_id, page: 1, pageSize: 10);

            // ASSERT
            var link = result.Items.Should().ContainSingle().Subject;
            link.Molde.Should().NotBeNull();
            link.Molde!.Numero.Should().Be("M-001");
        }

        [Test(Description = "TENCMREP2 - ExistsAssociation deve ignorar associacao excluida durante update.")]
        public async Task ExistsAssociationAsync_Should_IgnoreExcludedLink_When_UpdatingSameAssociation()
        {
            // ARRANGE
            await using var context = CreateContext();
            var link = new EncomendaMolde
            {
                Encomenda_id = 1,
                Molde_id = 2,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = DateTime.UtcNow.AddDays(10)
            };

            await context.EncomendasMoldes.AddAsync(link);
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.ExistsAssociationAsync(1, 2, link.EncomendaMolde_id);

            // ASSERT
            result.Should().BeFalse();
        }

        [Test(Description = "TENCMREP3 - GetFilaGlobalAbertos deve excluir encomendas concluidas e canceladas.")]
        public async Task GetFilaGlobalAbertosAsync_Should_ExcludeTerminalOrders()
        {
            // ARRANGE
            await using var context = CreateContext();
            var cliente = new Cliente { Nome = "Cliente Teste", NIF = "123456789", Sigla = "CLI" };

            var encomendaAberta = new Encomenda { NumeroEncomendaCliente = "ENC-ABERTA", Cliente = cliente, Estado = EstadoEncomenda.EM_PRODUCAO };
            var encomendaConcluida = new Encomenda { NumeroEncomendaCliente = "ENC-CONC", Cliente = cliente, Estado = EstadoEncomenda.CONCLUIDA };
            var encomendaCancelada = new Encomenda { NumeroEncomendaCliente = "ENC-CANC", Cliente = cliente, Estado = EstadoEncomenda.CANCELADA };

            var moldeA = new Molde { Numero = "M-001", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var moldeB = new Molde { Numero = "M-002", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var moldeC = new Molde { Numero = "M-003", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddRangeAsync(encomendaAberta, encomendaConcluida, encomendaCancelada);
            await context.Moldes.AddRangeAsync(moldeA, moldeB, moldeC);
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddRangeAsync(
                new EncomendaMolde
                {
                    Encomenda_id = encomendaAberta.Encomenda_id,
                    Molde_id = moldeA.Molde_id,
                    Quantidade = 1,
                    Prioridade = 1,
                    DataEntregaPrevista = new DateTime(2026, 6, 18)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomendaConcluida.Encomenda_id,
                    Molde_id = moldeB.Molde_id,
                    Quantidade = 1,
                    Prioridade = 2,
                    DataEntregaPrevista = new DateTime(2026, 6, 19)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomendaCancelada.Encomenda_id,
                    Molde_id = moldeC.Molde_id,
                    Quantidade = 1,
                    Prioridade = 3,
                    DataEntregaPrevista = new DateTime(2026, 6, 20)
                });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.GetFilaGlobalAbertosAsync();

            // ASSERT
            result.Should().ContainSingle();
            var item = result.Single();
            item.Encomenda.Should().NotBeNull();
            item.Molde.Should().NotBeNull();
            item.Encomenda!.NumeroEncomendaCliente.Should().Be("ENC-ABERTA");
            item.Molde!.Numero.Should().Be("M-001");
            item.Encomenda.Cliente!.Nome.Should().Be("Cliente Teste");
        }

        [Test(Description = "TENCMREP4 - GetFilaGlobal deve ordenar por prioridade e aplicar paginacao na base de dados.")]
        public async Task GetFilaGlobalAsync_Should_OrderAndPaginateOpenOrders()
        {
            // ARRANGE
            await using var context = CreateContext();
            var cliente = new Cliente { Nome = "Cliente Fila", NIF = "987654321", Sigla = "FIL" };
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-FILA", Cliente = cliente, Estado = EstadoEncomenda.EM_PRODUCAO };

            var moldeA = new Molde { Numero = "M-010", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var moldeB = new Molde { Numero = "M-011", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var moldeC = new Molde { Numero = "M-012", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddRangeAsync(moldeA, moldeB, moldeC);
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddRangeAsync(
                new EncomendaMolde
                {
                    Encomenda_id = encomenda.Encomenda_id,
                    Molde_id = moldeA.Molde_id,
                    Quantidade = 1,
                    Prioridade = 3,
                    DataEntregaPrevista = new DateTime(2026, 7, 1)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomenda.Encomenda_id,
                    Molde_id = moldeB.Molde_id,
                    Quantidade = 1,
                    Prioridade = 1,
                    DataEntregaPrevista = new DateTime(2026, 6, 18)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomenda.Encomenda_id,
                    Molde_id = moldeC.Molde_id,
                    Quantidade = 1,
                    Prioridade = 2,
                    DataEntregaPrevista = new DateTime(2026, 6, 27)
                });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.GetFilaGlobalAsync(page: 1, pageSize: 2);

            // ASSERT
            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(2);
            result.Items.Select(item => item.Prioridade).Should().ContainInOrder(1, 2);
            result.Items.First().Molde!.Numero.Should().Be("M-011");
            result.Items.Last().Molde!.Numero.Should().Be("M-012");
        }

        [Test(Description = "TENCMREP5 - GetByEncomendasConfirmadasParaDesenho deve devolver apenas moldes com projeto mais recente e ultima revisao aprovada.")]
        public async Task GetByEncomendasConfirmadasParaDesenhoAsync_Should_ReturnOnlyMoldesWithApprovedLatestProject()
        {
            // ARRANGE
            await using var context = CreateContext();
            var cliente = new Cliente { Nome = "Cliente Desenho", NIF = "111222333", Sigla = "DES" };
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-DES", Cliente = cliente, Estado = EstadoEncomenda.CONFIRMADA };
            var moldeA = new Molde { Numero = "M-100", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var moldeB = new Molde { Numero = "M-101", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddRangeAsync(moldeA, moldeB);
            await context.SaveChangesAsync();

            await context.Projetos.AddRangeAsync(
                new Projeto
                {
                    NomeProjeto = "Projeto aprovado",
                    SoftwareUtilizado = "NX",
                    TipoProjeto = TipoProjeto.PROJETO_3D,
                    CaminhoPastaServidor = @"\\srv\aprovado",
                    Molde_id = moldeA.Molde_id,
                    Revisoes =
                    {
                        new Revisao
                        {
                            NumRevisao = 1,
                            DescricaoAlteracoes = "Aprovado",
                            DataEnvioCliente = DateTime.UtcNow.AddDays(-2),
                            DataResposta = DateTime.UtcNow.AddDays(-1),
                            Aprovado = true
                        }
                    }
                },
                new Projeto
                {
                    NomeProjeto = "Projeto bloqueado",
                    SoftwareUtilizado = "NX",
                    TipoProjeto = TipoProjeto.PROJETO_3D,
                    CaminhoPastaServidor = @"\\srv\bloqueado",
                    Molde_id = moldeB.Molde_id,
                    Revisoes =
                    {
                        new Revisao
                        {
                            NumRevisao = 1,
                            DescricaoAlteracoes = "Bloqueado",
                            DataEnvioCliente = DateTime.UtcNow.AddDays(-2),
                            DataResposta = DateTime.UtcNow.AddDays(-1),
                            Aprovado = false
                        }
                    }
                });
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddRangeAsync(
                new EncomendaMolde
                {
                    Encomenda_id = encomenda.Encomenda_id,
                    Molde_id = moldeA.Molde_id,
                    Quantidade = 1,
                    Prioridade = 1,
                    DataEntregaPrevista = new DateTime(2026, 6, 20)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomenda.Encomenda_id,
                    Molde_id = moldeB.Molde_id,
                    Quantidade = 1,
                    Prioridade = 2,
                    DataEntregaPrevista = new DateTime(2026, 6, 21)
                });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.GetByEncomendasConfirmadasParaDesenhoAsync(page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(item => item.Molde!.Numero == "M-100");
        }

        [Test(Description = "TENCMREP6 - GetByEncomendasConfirmadasParaDesenho deve ignorar um molde quando o projeto mais recente nao tem a ultima revisao aprovada.")]
        public async Task GetByEncomendasConfirmadasParaDesenhoAsync_Should_IgnoreMold_When_LatestProjectIsNotApproved()
        {
            // ARRANGE
            await using var context = CreateContext();
            var cliente = new Cliente { Nome = "Cliente Bloqueio", NIF = "444555666", Sigla = "BLQ" };
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-BLQ", Cliente = cliente, Estado = EstadoEncomenda.CONFIRMADA };
            var molde = new Molde { Numero = "M-200", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.Projetos.AddAsync(new Projeto
            {
                NomeProjeto = "Projeto antigo aprovado",
                SoftwareUtilizado = "NX",
                TipoProjeto = TipoProjeto.PROJETO_3D,
                CaminhoPastaServidor = @"\\srv\antigo",
                Molde_id = molde.Molde_id,
                Revisoes =
                {
                    new Revisao
                    {
                        NumRevisao = 1,
                        DescricaoAlteracoes = "Aprovado",
                        DataEnvioCliente = DateTime.UtcNow.AddDays(-5),
                        DataResposta = DateTime.UtcNow.AddDays(-4),
                        Aprovado = true
                    }
                }
            });

            await context.Projetos.AddAsync(new Projeto
            {
                NomeProjeto = "Projeto mais recente bloqueado",
                SoftwareUtilizado = "NX",
                TipoProjeto = TipoProjeto.PROJETO_3D,
                CaminhoPastaServidor = @"\\srv\recente",
                Molde_id = molde.Molde_id,
                Revisoes =
                {
                    new Revisao
                    {
                        NumRevisao = 1,
                        DescricaoAlteracoes = "A aguardar",
                        DataEnvioCliente = DateTime.UtcNow.AddDays(-2),
                        DataResposta = DateTime.UtcNow.AddDays(-1),
                        Aprovado = false
                    }
                }
            });
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddAsync(new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = new DateTime(2026, 6, 22)
            });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.GetByEncomendasConfirmadasParaDesenhoAsync(page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }

        [Test(Description = "TENCMREP7 - SearchByTermForDesenho deve filtrar apenas as associacoes elegiveis que correspondem ao termo.")]
        public async Task SearchByTermForDesenhoAsync_Should_FilterByCustomerName_When_SearchTermMatches()
        {
            // ARRANGE
            await using var context = CreateContext();
            var clienteA = new Cliente { Nome = "Cliente Apto", NIF = "777888999", Sigla = "APT" };
            var clienteB = new Cliente { Nome = "Cliente Outro", NIF = "111000999", Sigla = "OUT" };

            var encomendaA = new Encomenda { NumeroEncomendaCliente = "ENC-APT", Cliente = clienteA, Estado = EstadoEncomenda.CONFIRMADA };
            var encomendaB = new Encomenda { NumeroEncomendaCliente = "ENC-OUT", Cliente = clienteB, Estado = EstadoEncomenda.CONFIRMADA };

            var moldeA = new Molde { Numero = "M-300", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE, Nome = "Molde Apto", Descricao = "Descricao valida" };
            var moldeB = new Molde { Numero = "M-301", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE, Nome = "Molde Outro", Descricao = "Outra descricao" };

            await context.Encomendas.AddRangeAsync(encomendaA, encomendaB);
            await context.Moldes.AddRangeAsync(moldeA, moldeB);
            await context.SaveChangesAsync();

            await context.Projetos.AddRangeAsync(
                new Projeto
                {
                    NomeProjeto = "Projeto A",
                    SoftwareUtilizado = "NX",
                    TipoProjeto = TipoProjeto.PROJETO_3D,
                    CaminhoPastaServidor = @"\\srv\a",
                    Molde_id = moldeA.Molde_id,
                    Revisoes =
                    {
                        new Revisao
                        {
                            NumRevisao = 1,
                            DescricaoAlteracoes = "Aprovado",
                            DataEnvioCliente = DateTime.UtcNow.AddDays(-3),
                            DataResposta = DateTime.UtcNow.AddDays(-2),
                            Aprovado = true
                        }
                    }
                },
                new Projeto
                {
                    NomeProjeto = "Projeto B",
                    SoftwareUtilizado = "NX",
                    TipoProjeto = TipoProjeto.PROJETO_3D,
                    CaminhoPastaServidor = @"\\srv\b",
                    Molde_id = moldeB.Molde_id,
                    Revisoes =
                    {
                        new Revisao
                        {
                            NumRevisao = 1,
                            DescricaoAlteracoes = "Aprovado",
                            DataEnvioCliente = DateTime.UtcNow.AddDays(-3),
                            DataResposta = DateTime.UtcNow.AddDays(-2),
                            Aprovado = true
                        }
                    }
                });
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddRangeAsync(
                new EncomendaMolde
                {
                    Encomenda_id = encomendaA.Encomenda_id,
                    Molde_id = moldeA.Molde_id,
                    Quantidade = 1,
                    Prioridade = 1,
                    DataEntregaPrevista = new DateTime(2026, 6, 20)
                },
                new EncomendaMolde
                {
                    Encomenda_id = encomendaB.Encomenda_id,
                    Molde_id = moldeB.Molde_id,
                    Quantidade = 1,
                    Prioridade = 2,
                    DataEntregaPrevista = new DateTime(2026, 6, 21)
                });
            await context.SaveChangesAsync();

            var repository = new EncomendaMoldeRepository(context);

            // ACT
            var result = await repository.SearchByTermForDesenhoAsync("apto", page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
            var item = result.Items.Single();
            item.Encomenda!.Cliente!.Nome.Should().Be("Cliente Apto");
            item.Molde!.Numero.Should().Be("M-300");
        }
    }

}
