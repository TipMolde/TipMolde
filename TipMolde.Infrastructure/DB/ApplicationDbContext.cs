using Microsoft.EntityFrameworkCore;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Infrastructure.DB
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public virtual DbSet<RevokedToken> RevokedTokens { get; set; }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Cliente> Clientes { get; set; }
        public virtual DbSet<Molde> Moldes { get; set; }
        public virtual DbSet<EspecificacoesTecnicas> EspecificacoesTecnicas { get; set; }
        public virtual DbSet<Encomenda> Encomendas { get; set; }
        public virtual DbSet<EncomendaMolde> EncomendasMoldes { get; set; }
        public virtual DbSet<Peca> Pecas { get; set; }
        public virtual DbSet<Maquina> Maquinas { get; set; }
        public virtual DbSet<Fornecedor> Fornecedores { get; set; }
        public virtual DbSet<PedidoMaterial> PedidosMaterial { get; set; }
        public virtual DbSet<ItemPedidoMaterial> ItensPedidoMaterial { get; set; }
        public virtual DbSet<FasesProducao> Fases_Producao { get; set; }
        public virtual DbSet<RegistosProducao> RegistosProducao { get; set; }
        public virtual DbSet<Projeto> Projetos { get; set; }
        public virtual DbSet<Revisao> Revisoes { get; set; }
        public virtual DbSet<RegistoTempoProjeto> RegistosTempoProjeto { get; set; }
        public virtual DbSet<FichaProducao> FichasProducao { get; set; }
        public virtual DbSet<FichaDocumento> FichasDocumentos { get; set; }
        public virtual DbSet<FichaFrmLinha> FichasFrmLinhas { get; set; }
        public virtual DbSet<FichaFraLinha> FichasFraLinhas { get; set; }
        public virtual DbSet<FichaFopLinha> FichasFopLinhas { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>().HasKey(x => x.User_id);
            modelBuilder.Entity<Cliente>().HasKey(x => x.Cliente_id);
            modelBuilder.Entity<Encomenda>().HasKey(x => x.Encomenda_id);
            modelBuilder.Entity<Molde>().HasKey(x => x.Molde_id);
            modelBuilder.Entity<EspecificacoesTecnicas>().HasKey(x => x.Molde_id);
            modelBuilder.Entity<EncomendaMolde>().HasKey(x => x.EncomendaMolde_id);
            modelBuilder.Entity<Peca>().HasKey(x => x.Peca_id);
            modelBuilder.Entity<Fornecedor>().HasKey(x => x.Fornecedor_id);
            modelBuilder.Entity<PedidoMaterial>().HasKey(x => x.PedidoMaterial_id);
            modelBuilder.Entity<FasesProducao>().HasKey(x => x.Fases_producao_id);
            modelBuilder.Entity<RegistosProducao>().HasKey(x => x.Registo_Producao_id);
            modelBuilder.Entity<Projeto>().HasKey(x => x.Projeto_id);
            modelBuilder.Entity<Revisao>().HasKey(x => x.Revisao_id);
            modelBuilder.Entity<RegistoTempoProjeto>().HasKey(x => x.Registo_Tempo_Projeto_id);
            modelBuilder.Entity<FichaProducao>().HasKey(x => x.FichaProducao_id);
            modelBuilder.Entity<FichaDocumento>().HasKey(x => x.FichaDocumento_id);
            modelBuilder.Entity<FichaFrmLinha>().HasKey(x => x.FichaFrmLinha_id);
            modelBuilder.Entity<FichaFraLinha>().HasKey(x => x.FichaFraLinha_id);
            modelBuilder.Entity<FichaFopLinha>().HasKey(x => x.FichaFopLinha_id);
            modelBuilder.Entity<RevokedToken>().HasKey(x => x.RevokedToken_id);
            modelBuilder.Entity<Maquina>().HasKey(m => m.Maquina_id);
            modelBuilder.Entity<ItemPedidoMaterial>().HasKey(i => new { i.PedidoMaterial_id, i.Peca_id });


            modelBuilder.Entity<RevokedToken>()
                .HasIndex(x => x.Jti)
                .IsUnique();

            modelBuilder.Entity<RevokedToken>()
                .Property(x => x.Jti)
                .HasMaxLength(200);


            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.NIF)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Sigla)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Molde>()
                .HasIndex(m => m.Numero)
                .IsUnique();

            modelBuilder.Entity<Encomenda>()
                .HasIndex(e => e.NumeroEncomendaCliente)
                .IsUnique();

            modelBuilder.Entity<Encomenda>()
                .Property(e => e.Estado).HasConversion<string>().HasMaxLength(30);

            modelBuilder.Entity<Encomenda>()
                .HasOne(e => e.Cliente)
                .WithMany(c => c.Encomendas)
                .HasForeignKey(e => e.Cliente_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EspecificacoesTecnicas>()
                .HasOne(e => e.Molde)
                .WithOne(m => m.Especificacoes)
                .HasForeignKey<EspecificacoesTecnicas>(e => e.Molde_id);

            modelBuilder.Entity<EncomendaMolde>()
                .HasIndex(em => new { em.Encomenda_id, em.Molde_id })
                .IsUnique();

            modelBuilder.Entity<EncomendaMolde>()
                .HasOne(em => em.Encomenda)
                .WithMany(e => e.EncomendasMoldes)
                .HasForeignKey(em => em.Encomenda_id);

            modelBuilder.Entity<EncomendaMolde>()
                .HasOne(em => em.Molde)
                .WithMany(m => m.EncomendasMoldes)
                .HasForeignKey(em => em.Molde_id);

            modelBuilder.Entity<Peca>()
                .Property(p => p.NumeroPeca)
                .HasMaxLength(50);

            modelBuilder.Entity<Peca>()
                .Property(p => p.Designacao)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Peca>()
                .Property(p => p.Referencia)
                .HasMaxLength(200);

            modelBuilder.Entity<Peca>()
                .Property(p => p.MaterialDesignacao)
                .HasMaxLength(100);

            modelBuilder.Entity<Peca>()
                .Property(p => p.TratamentoTermico)
                .HasMaxLength(100);

            modelBuilder.Entity<Peca>()
                .Property(p => p.Massa)
                .HasMaxLength(50);

            modelBuilder.Entity<Peca>()
                .Property(p => p.Observacao)
                .HasMaxLength(100);

            modelBuilder.Entity<Fornecedor>()
                .HasIndex(f => f.NIF)
                .IsUnique();

            modelBuilder.Entity<PedidoMaterial>()
                .HasMany(p => p.Itens)
                .WithOne(i => i.PedidoMaterial)
                .HasForeignKey(i => i.PedidoMaterial_id);

            modelBuilder.Entity<Maquina>()
                .Property(m => m.Estado).HasConversion<string>().HasMaxLength(30);

            modelBuilder.Entity<Maquina>()
                .HasIndex(m => m.Numero)
                .IsUnique();

            modelBuilder.Entity<Maquina>()
                .HasOne(m => m.FaseDedicada)
                .WithMany(f => f.MaquinasDedicadas)
                .HasForeignKey(m => m.FaseDedicada_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FasesProducao>()
                .HasIndex(f => f.Nome)
                .IsUnique();

            modelBuilder.Entity<FasesProducao>()
                .Property(f => f.Descricao)
                .HasMaxLength(255);

            modelBuilder.Entity<RegistosProducao>()
                .HasOne(r => r.Maquina)
                .WithMany()
                .HasForeignKey(r => r.Maquina_id);

            modelBuilder.Entity<RegistosProducao>()
                .Property(r => r.Estado_producao).HasConversion<string>().HasMaxLength(30);

            modelBuilder.Entity<FichaProducao>()
                .Property(f => f.Tipo).HasConversion<string>().HasMaxLength(10);

            modelBuilder.Entity<FichaProducao>()
                .HasOne(f => f.EncomendaMolde)
                .WithMany(em => em.Fichas)
                .HasForeignKey(f => f.EncomendaMolde_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FichaDocumento>()
                .HasOne(x => x.FichaProducao)
                .WithMany(f => f.Relatorios)
                .HasForeignKey(x => x.FichaProducao_id);

            modelBuilder.Entity<FichaDocumento>()
                .HasIndex(x => new { x.FichaProducao_id, x.Versao })
                .IsUnique();

            modelBuilder.Entity<FichaDocumento>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.CriadoPor_user_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FichaFrmLinha>()
                .Property(x => x.Defeito)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<FichaFrmLinha>()
                .Property(x => x.Pormenor)
                .HasMaxLength(4000);

            modelBuilder.Entity<FichaFrmLinha>()
                .HasOne(x => x.FichaProducao)
                .WithMany()
                .HasForeignKey(x => x.FichaProducao_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FichaFrmLinha>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.Responsavel_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FichaFraLinha>()
                .Property(x => x.Alteracoes)
                .HasMaxLength(4000)
                .IsRequired();

            modelBuilder.Entity<FichaFraLinha>()
                .HasOne(x => x.FichaProducao)
                .WithMany()
                .HasForeignKey(x => x.FichaProducao_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FichaFraLinha>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.Responsavel_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FichaFopLinha>()
                .Property(x => x.Ocorrencia)
                .HasMaxLength(4000)
                .IsRequired();

            modelBuilder.Entity<FichaFopLinha>()
                .Property(x => x.Correcao)
                .HasMaxLength(4000);

            modelBuilder.Entity<FichaFopLinha>()
                .HasOne(x => x.FichaProducao)
                .WithMany()
                .HasForeignKey(x => x.FichaProducao_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FichaFopLinha>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.Responsavel_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Projeto>()
                .HasOne(p => p.Molde)
                .WithMany()
                .HasForeignKey(p => p.Molde_id);

            modelBuilder.Entity<Projeto>()
                .Property(p => p.NomeProjeto)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Projeto>()
                .Property(p => p.SoftwareUtilizado)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Projeto>()
                .Property(p => p.CaminhoPastaServidor)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<Projeto>()
                .Property(p => p.TipoProjeto)
                .HasConversion(
                    v => v == TipoProjeto.PROJETO_2D ? "2D" : "3D",
                    v => v == "2D" ? TipoProjeto.PROJETO_2D : TipoProjeto.PROJETO_3D)
                .HasMaxLength(10);

            modelBuilder.Entity<Revisao>()
                 .HasOne(r => r.Projeto)
                 .WithMany(p => p.Revisoes)
                 .HasForeignKey(r => r.Projeto_id);

            modelBuilder.Entity<Revisao>()
                .Property(r => r.DescricaoAlteracoes)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<Revisao>()
                .Property(r => r.FeedbackTexto)
                .HasMaxLength(4000);

            modelBuilder.Entity<Revisao>()
                .Property(r => r.FeedbackImagemPath)
                .HasMaxLength(255);

            modelBuilder.Entity<Revisao>()
                .HasIndex(r => new { r.Projeto_id, r.NumRevisao })
                .IsUnique();

            modelBuilder.Entity<RegistoTempoProjeto>()
                .Property(r => r.Estado_tempo)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<RegistoTempoProjeto>()
                .HasIndex(r => new { r.Projeto_id, r.Autor_id, r.Data_hora, r.Registo_Tempo_Projeto_id });

            modelBuilder.Entity<RegistoTempoProjeto>()
                .HasOne(r => r.Projeto)
                .WithMany(p => p.RegistosTempo)
                .HasForeignKey(r => r.Projeto_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistoTempoProjeto>()
                .HasOne(r => r.Autor)
                .WithMany()
                .HasForeignKey(r => r.Autor_id)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
