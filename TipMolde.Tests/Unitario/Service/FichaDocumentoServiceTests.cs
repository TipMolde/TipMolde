using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Infrastructure.Service;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class FichaDocumentoServiceTests
{
    private Mock<IFichaDocumentoRepository> _repository = null!;
    private Mock<IFichaDocumentoStorage> _storage = null!;
    private Mock<IFichaDocumentoUnitOfWork> _unitOfWork = null!;
    private Mock<ILogger<FichaDocumentoService>> _logger = null!;
    private IMapper _mapper = null!;
    private FichaDocumentoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IFichaDocumentoRepository>();
        _storage = new Mock<IFichaDocumentoStorage>();
        _unitOfWork = new Mock<IFichaDocumentoUnitOfWork>();
        _logger = new Mock<ILogger<FichaDocumentoService>>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaDocumentoProfile>());
        _mapper = config.CreateMapper();

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<ResponseFichaDocumentoDto>>>()))
            .Returns((Func<Task<ResponseFichaDocumentoDto>> action) => action());

        _sut = new FichaDocumentoService(
            _repository.Object,
            _storage.Object,
            _unitOfWork.Object,
            _logger.Object,
            _mapper);
    }

    [Test(Description = "TFDOCSRV1 - GuardarGerado deve criar nome versionado, persistir ficheiro e registar metadados.")]
    public async Task GuardarGeradoAsync_Should_SaveVersionedFileAndMetadata_When_RequestIsValid()
    {
        var content = new byte[] { 1, 2, 3, 4 };

        _repository.Setup(r => r.FichaExisteAsync(7)).ReturnsAsync(true);
        _repository.Setup(r => r.GetProximaVersaoAsync(7)).ReturnsAsync(3);
        _repository.Setup(r => r.DesativarVersoesAtivasAsync(7)).Returns(Task.CompletedTask);
        _storage.Setup(s => s.SaveAsync(7, "relatorio_v3.pdf", content))
            .ReturnsAsync("Storage\\Fichas\\7\\relatorio_v3.pdf");
        _repository.Setup(r => r.AddAsync(It.IsAny<FichaDocumento>()))
            .Callback<FichaDocumento>(doc => doc.FichaDocumento_id = 55)
            .Returns(Task.CompletedTask);

        var result = await _sut.GuardarGeradoAsync(7, content, "relatorio.pdf", "application/pdf", 12, "SISTEMA");

        result.FichaDocumento_id.Should().Be(55);
        result.FichaProducao_id.Should().Be(7);
        result.CriadoPor_user_id.Should().Be(12);
        result.Versao.Should().Be(3);
        result.NomeFicheiro.Should().Be("relatorio_v3.pdf");
        result.TipoFicheiro.Should().Be("application/pdf");
        result.Origem.Should().Be("SISTEMA");
        result.Ativo.Should().BeTrue();

        _repository.Verify(r => r.DesativarVersoesAtivasAsync(7), Times.Once);
        _storage.Verify(s => s.SaveAsync(7, "relatorio_v3.pdf", content), Times.Once);
        _repository.Verify(r => r.AddAsync(It.Is<FichaDocumento>(doc =>
            doc.FichaProducao_id == 7 &&
            doc.CriadoPor_user_id == 12 &&
            doc.Versao == 3 &&
            doc.NomeFicheiro == "relatorio_v3.pdf" &&
            doc.CaminhoFicheiro == "Storage\\Fichas\\7\\relatorio_v3.pdf" &&
            doc.HashSha256 != null &&
            doc.HashSha256.Length == 64 &&
            doc.Ativo)),
            Times.Once);
    }

    [Test(Description = "TFDOCSRV2 - GuardarGerado deve falhar quando a ficha nao existe.")]
    public async Task GuardarGeradoAsync_Should_ThrowKeyNotFoundException_When_FichaDoesNotExist()
    {
        _repository.Setup(r => r.FichaExisteAsync(99)).ReturnsAsync(false);

        Func<Task> act = () => _sut.GuardarGeradoAsync(99, new byte[] { 1 }, "ficha.pdf", "application/pdf", 1, "UPLOAD");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    [TestCase("malicioso.exe", "application/pdf", Description = "TFDOCSRV3 - GuardarGerado deve rejeitar extensoes nao suportadas.")]
    [TestCase("ficha.pdf", "text/plain", Description = "TFDOCSRV4 - GuardarGerado deve rejeitar tipos MIME nao suportados.")]
    public async Task GuardarGeradoAsync_Should_ThrowArgumentException_When_FileIsNotSupported(string fileName, string contentType)
    {
        _repository.Setup(r => r.FichaExisteAsync(7)).ReturnsAsync(true);

        Func<Task> act = () => _sut.GuardarGeradoAsync(7, new byte[] { 1, 2 }, fileName, contentType, 3, "UPLOAD");

        await act.Should().ThrowAsync<ArgumentException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test(Description = "TFDOCSRV5 - GuardarGerado deve limpar o ficheiro fisico quando a transacao falha apos a escrita.")]
    public async Task GuardarGeradoAsync_Should_DeletePhysicalFile_When_TransactionFailsAfterSave()
    {
        var content = new byte[] { 10, 20 };

        _repository.Setup(r => r.FichaExisteAsync(8)).ReturnsAsync(true);
        _repository.Setup(r => r.GetProximaVersaoAsync(8)).ReturnsAsync(2);
        _repository.Setup(r => r.DesativarVersoesAtivasAsync(8)).Returns(Task.CompletedTask);
        _storage.Setup(s => s.SaveAsync(8, "documento_v2.pdf", content))
            .ReturnsAsync("Storage\\Fichas\\8\\documento_v2.pdf");
        _repository.Setup(r => r.AddAsync(It.IsAny<FichaDocumento>()))
            .ThrowsAsync(new InvalidOperationException("Falha a persistir metadata."));

        Func<Task> act = () => _sut.GuardarGeradoAsync(8, content, "documento.pdf", "application/pdf", 2, "SISTEMA");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*metadata*");

        _storage.Verify(s => s.DeleteIfExistsAsync("Storage\\Fichas\\8\\documento_v2.pdf"), Times.Once);
    }

    [Test(Description = "TFDOCSRV6 - Download deve falhar quando os metadados existem mas o ficheiro fisico nao esta no disco.")]
    public async Task DownloadAsync_Should_ThrowFileNotFoundException_When_StoredFileDoesNotExist()
    {
        _repository.Setup(r => r.GetByIdAndFichaIdAsync(5, 14))
            .ReturnsAsync(new FichaDocumento
            {
                FichaDocumento_id = 14,
                FichaProducao_id = 5,
                NomeFicheiro = "ficha_v1.pdf",
                TipoFicheiro = "application/pdf",
                CaminhoFicheiro = "Storage\\Fichas\\5\\ficha_v1.pdf"
            });
        _storage.Setup(s => s.Exists("Storage\\Fichas\\5\\ficha_v1.pdf")).Returns(false);

        Func<Task> act = () => _sut.DownloadAsync(5, 14);

        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*ficha_v1.pdf*");
    }

    [Test(Description = "TFDOCSRV7 - Listar deve normalizar pagina e tamanho antes de consultar o repositorio.")]
    public async Task ListarAsync_Should_NormalizePaginationArguments()
    {
        _repository.Setup(r => r.FichaExisteAsync(4)).ReturnsAsync(true);
        _repository.Setup(r => r.GetByFichaIdAsync(4, 1, 10))
            .ReturnsAsync(new PagedResult<FichaDocumento>(
                new[]
                {
                    new FichaDocumento
                    {
                        FichaDocumento_id = 1,
                        FichaProducao_id = 4,
                        CriadoPor_user_id = 2,
                        Versao = 1,
                        Origem = "UPLOAD",
                        NomeFicheiro = "ficha_v1.pdf",
                        TipoFicheiro = "application/pdf",
                        Ativo = true
                    }
                },
                1,
                1,
                10));

        var result = await _sut.ListarAsync(4, 0, -5);

        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().ContainSingle();
        result.Items.Single().NomeFicheiro.Should().Be("ficha_v1.pdf");
    }
}
