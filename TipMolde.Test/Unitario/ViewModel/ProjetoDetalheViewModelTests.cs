using System.Reflection;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class ProjetoDetalheViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private ProjetoDetalheViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();

        var httpClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var sessaoPersistidaService = new SessaoPersistidaService(httpClient);
        var utilizadoresService = new UtilizadoresService(httpClient);
        var authorizationService = new AuthorizationService(sessaoPersistidaService, utilizadoresService);

        _sut = new ProjetoDetalheViewModel(
            new ProjetosService(httpClient),
            new RevisoesService(httpClient),
            new RegistosTempoProjetoService(httpClient),
            authorizationService,
            sessaoPersistidaService,
            _dialogService.Object);

        _sut.CanManageTempo = true;
        _sut.IsAdmin = true;
        _sut.Projeto = new ProjetoComRevisoesDto
        {
            Projeto_id = 77,
            NomeProjeto = "Projeto de teste",
            SoftwareUtilizado = "SolidWorks",
            TipoProjeto = "PROJETO_3D",
            CaminhoPastaServidor = @"\\srv\proj"
        };

        SetPrivateField("_currentUserId", 15);
    }

    [Test]
    public async Task RegistarTempoAsync_Should_ShowOnlyAllowedStates_When_HistoricoHasStartedAndPaused()
    {
        // ARRANGE
        _sut.RegistosTempo.Add(new RegistoTempoProjetoDto
        {
            Estado_tempo = "INICIADO",
            Data_hora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
        });
        _sut.RegistosTempo.Add(new RegistoTempoProjetoDto
        {
            Estado_tempo = "PAUSADO",
            Data_hora = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc)
        });

        string[]? selectedOptions = null;
        _dialogService
            .Setup(s => s.ShowSelectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Callback<string, string, string[]>((_, _, options) => selectedOptions = options)
            .ReturnsAsync((string?)null);

        // ACT
        await _sut.RegistarTempoCommand.ExecuteAsync(null);

        // ASSERT
        selectedOptions.Should().NotBeNull();
        selectedOptions.Should().Equal("RETOMADO");
        _dialogService.Verify(
            s => s.ShowSelectionAsync(
                "Registar tempo para Projeto de teste",
                "Cancelar",
                It.IsAny<string[]>()),
            Times.Once);
    }

    [Test]
    public async Task RegistarTempoAsync_Should_AllowRestart_When_ProjectIsConcluded()
    {
        // ARRANGE
        _sut.RegistosTempo.Add(new RegistoTempoProjetoDto
        {
            Estado_tempo = "CONCLUIDO",
            Data_hora = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        });

        string[]? selectedOptions = null;
        _dialogService
            .Setup(s => s.ShowSelectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Callback<string, string, string[]>((_, _, options) => selectedOptions = options)
            .ReturnsAsync((string?)null);

        // ACT
        await _sut.RegistarTempoCommand.ExecuteAsync(null);

        // ASSERT
        selectedOptions.Should().Equal("INICIADO");
        _dialogService.Verify(
            s => s.ShowSelectionAsync(
                "Registar tempo para Projeto de teste",
                "Cancelar",
                It.IsAny<string[]>()),
            Times.Once);
    }

    [Test]
    public void CanCreateRevisao_Should_BeTrue_When_ProjectHasNoRevisions()
    {
        _sut.CanCreateRevisao.Should().BeTrue();
    }

    [Test]
    public void CanCreateRevisao_Should_BeFalse_When_LastRevisionIsOpenOrApproved()
    {
        _sut.Projeto!.Revisoes.Add(new RevisaoDto
        {
            Revisao_id = 1,
            NumRevisao = 2,
            Aprovado = true,
            DataResposta = DateTime.UtcNow.AddMinutes(-5)
        });

        _sut.CanCreateRevisao.Should().BeFalse();
    }

    private void SetPrivateField<T>(string fieldName, T value)
    {
        typeof(ProjetoDetalheViewModel)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, value);
    }
}
