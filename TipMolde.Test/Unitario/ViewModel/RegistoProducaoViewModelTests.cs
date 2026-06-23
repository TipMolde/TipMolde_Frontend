using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class RegistoProducaoViewModelTests
{
    private RegistoProducaoViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        _sut = new RegistoProducaoViewModel(
            new RegistoProducaoViewModelDependencies(
                new RegistosProducaoService(httpClient),
                new FasesProducaoService(httpClient),
                new MaquinasService(httpClient),
                new PecasService(httpClient),
                new MoldesService(httpClient)),
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            new DialogServiceStub());
    }

    [Test]
    public void AtualizarResumoTempoProducao_Should_SomarIntervalosAtivosDaPeca()
    {
        // ARRANGE
        _sut.PecaContexto = new ProducaoPecaDisponivelItem
        {
            PecaId = 42
        };

        SetPrivateField("_todosRegistos", new List<RegistoProducaoDto>
        {
            new()
            {
                PecaId = 42,
                EstadoProducao = "PREPARACAO",
                DataHora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 42,
                EstadoProducao = "EM_CURSO",
                DataHora = new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 42,
                EstadoProducao = "PAUSADO",
                DataHora = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 42,
                EstadoProducao = "EM_CURSO",
                DataHora = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 42,
                EstadoProducao = "CONCLUIDO",
                DataHora = new DateTime(2026, 6, 1, 13, 15, 0, DateTimeKind.Utc)
            }
        });

        // ACT
        InvokePrivateMethod("AtualizarResumoTempoProducao");

        // ASSERT
        _sut.TempoTotalProducaoDisplay.Should().Be("2h 15m");
        _sut.TempoSessaoAtivaProducaoDisplay.Should().Be("Sem sessao ativa");
    }

    [Test]
    public void CanRegistarOcorrencia_Should_BeTrue_When_StateIsPreparacao()
    {
        // ARRANGE
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "PREPARACAO",
            DisplayName = "PREPARACAO"
        };

        // ASSERT
        _sut.CanRegistarOcorrencia.Should().BeTrue();
    }

    [Test]
    public void CanRegistarOcorrencia_Should_BeTrue_When_StateIsPausado()
    {
        // ARRANGE
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "PAUSADO",
            DisplayName = "PAUSADO"
        };

        // ASSERT
        _sut.CanRegistarOcorrencia.Should().BeTrue();
    }

    [Test]
    public void CanRegistarCorrecao_Should_BeFalse_When_OcorrenciaIsEmpty()
    {
        // ARRANGE
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "EM_CURSO",
            DisplayName = "EM CURSO"
        };

        // ASSERT
        _sut.CanRegistarCorrecao.Should().BeFalse();
    }

    [Test]
    public void CanRegistarOcorrencia_Should_BeFalse_When_StateIsPendente()
    {
        // ARRANGE
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "PENDENTE",
            DisplayName = "PENDENTE"
        };

        // ASSERT
        _sut.CanRegistarOcorrencia.Should().BeFalse();
    }

    [Test]
    public void GetEstadosDisponiveis_Should_AllowPreparacao_AfterConcluido()
    {
        // ARRANGE
        var fase = new FaseProducaoItem
        {
            FasesProducao_id = 3,
            Nome = "MAQUINACAO"
        };

        var registos = new Dictionary<int, RegistoProducaoDto?>
        {
            [3] = new RegistoProducaoDto
            {
                FaseId = 3,
                EstadoProducao = "CONCLUIDO",
                DataHora = new DateTime(2026, 6, 1, 13, 15, 0, DateTimeKind.Utc)
            }
        };

        _sut.PecaContexto = new ProducaoPecaDisponivelItem
        {
            PecaId = 42,
            EncomendaMolde_id = 11,
            ProximaFaseId = 3,
            UltimosRegistosPorFase = registos
        };
        SetPrivateField("_todasFases", new List<FaseProducaoItem> { fase });

        // ACT
        var method = typeof(RegistoProducaoViewModel).GetMethod(
            "GetEstadosDisponiveis",
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(Dictionary<int, RegistoProducaoDto?>), typeof(FaseProducaoItem)])!;

        var result = (List<EstadoProducaoOption>)method.Invoke(_sut, [registos, fase])!;

        // ASSERT
        result.Should().ContainSingle(item => item.Value == "PREPARACAO");
    }

    [Test]
    public void CanRegistarCorrecao_Should_BeTrue_When_OcorrenciaExists()
    {
        // ARRANGE
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "EM_CURSO",
            DisplayName = "EM CURSO"
        };
        _sut.Ocorrencia = "Paragem por ajuste";

        // ASSERT
        _sut.CanRegistarCorrecao.Should().BeTrue();
    }

    [Test]
    public void CanGuardar_Should_Not_Require_Ocorrencia_When_StateAllowsIt()
    {
        // ARRANGE
        _sut.GestorProducaoId = 1;
        _sut.PecaContexto = new ProducaoPecaDisponivelItem
        {
            PecaId = 7,
            EncomendaMolde_id = 11
        };
        _sut.SelectedFase = new FaseProducaoItem
        {
            FasesProducao_id = 3,
            Nome = "MAQUINACAO"
        };
        _sut.SelectedEstado = new EstadoProducaoOption
        {
            Value = "EM_CURSO",
            DisplayName = "EM CURSO"
        };

        // ASSERT
        _sut.CanGuardar.Should().BeTrue();
    }

    private void InvokePrivateMethod(string methodName)
    {
        typeof(RegistoProducaoViewModel)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(_sut, null);
    }

    private void SetPrivateField<T>(string fieldName, T value)
    {
        typeof(RegistoProducaoViewModel)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, value);
    }

    private sealed class DialogServiceStub : IDialogService
    {
        public Page GetCurrentPage() => new ContentPage();
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
