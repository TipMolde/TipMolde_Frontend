using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
[Category("MVVM")]
public class MaquinasViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private Mock<INavigationService> _navigationService = null!;
    private MaquinasViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();
        _navigationService = new Mock<INavigationService>();

        var httpClient = CreateHttpClient(HandleRequest);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId: 1));

        _sut = new MaquinasViewModel(
            new MaquinasService(httpClient),
            new FasesProducaoService(httpClient),
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)),
            _dialogService.Object,
            _navigationService.Object);
    }

    [Test(Description = "T1MAQ - A pagina de maquinas deve mostrar apenas cinco registos por pagina e calcular os totais do resumo.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_ShowFiveMachinesPerPageAndSummaryTotals()
    {
        await _sut.LoadAsync();

        _sut.Maquinas.Should().HaveCount(5);
        _sut.PageSize.Should().Be(5);
        _sut.TotalPages.Should().Be(2);
        _sut.TotalItems.Should().Be(7);
        _sut.Maquinas.Select(item => item.Maquina_id).Should().ContainInOrder(1, 2, 3, 4, 5);
        _sut.TotalMaquinasDisponiveis.Should().Be(3);
        _sut.TotalMaquinasEmUso.Should().Be(2);
        _sut.TotalMaquinasManutencao.Should().Be(2);
        _sut.TotalMaquinasComConexao.Should().Be(4);
    }

    [Test(Description = "T2MAQ - A paginacao da listagem principal deve permitir avancar para os registos restantes.")]
    public async Task NextPageCommand_Should_ShowRemainingMachines()
    {
        await _sut.LoadAsync();

        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(2);
        _sut.Maquinas.Should().HaveCount(2);
        _sut.Maquinas.Select(item => item.Maquina_id).Should().ContainInOrder(6, 7);
    }

    [Test(Description = "T3MAQ - O comando de detalhes nao deve navegar quando a maquina nao tem IP configurado.")]
    public async Task AbrirDetalhesCommand_Should_NotNavigate_When_MachineHasNoIp()
    {
        await _sut.LoadAsync();
        var machineWithoutIp = _sut.Maquinas.Single(item => item.Maquina_id == 2);

        await _sut.AbrirDetalhesCommand.ExecuteAsync(machineWithoutIp);

        _navigationService.Verify(service => service.GoToAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, object>>()),
            Times.Never);
    }

    [Test(Description = "T4MAQ - O comando de detalhes deve navegar para o detalhe da maquina quando existe IP configurado.")]
    public async Task AbrirDetalhesCommand_Should_Navigate_When_MachineHasIp()
    {
        await _sut.LoadAsync();
        var machineWithIp = _sut.Maquinas.Single(item => item.Maquina_id == 1);

        await _sut.AbrirDetalhesCommand.ExecuteAsync(machineWithIp);

        _navigationService.Verify(service => service.GoToAsync(
            nameof(MaquinaDetalhePage),
            It.Is<IDictionary<string, object>>(parameters =>
                parameters.ContainsKey("maquina_id") &&
                parameters["maquina_id"].Equals(1))),
            Times.Once);
    }

    [Test(Description = "T5MAQ - A pesquisa deve recarregar a listagem paginada com os resultados filtrados do backend.")]
    public async Task PesquisarCommand_Should_ApplyFilterAndResetPagination()
    {
        await _sut.LoadAsync();
        _sut.SearchTerm = "Laser";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(1);
        _sut.TotalPages.Should().Be(1);
        _sut.TotalItems.Should().Be(2);
        _sut.Maquinas.Should().HaveCount(2);
        _sut.Maquinas.Select(item => item.Maquina_id).Should().ContainInOrder(6, 7);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/users/me" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 1,
                    Nome = "Administrador",
                    Email = "admin@tipmolde.pt",
                    Role = "ADMIN"
                }),
            "/api/fases-producao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FaseProducaoItem>
                {
                    Items =
                    [
                        new FaseProducaoItem { FasesProducao_id = 1, Nome = "MAQUINACAO", Descricao = "Fase 1" },
                        new FaseProducaoItem { FasesProducao_id = 2, Nome = "EROSAO", Descricao = "Fase 2" },
                        new FaseProducaoItem { FasesProducao_id = 3, Nome = "MONTAGEM", Descricao = "Fase 3" }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 3
                }),
            "/api/Maquina?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<MaquinaItem>
                {
                    Items = CreateMachines(),
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 7
                }),
            "/api/Maquina/search?searchTerm=Laser&page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<MaquinaItem>
                {
                    Items = CreateMachines().Where(item => item.NomeModelo.Contains("Laser", StringComparison.OrdinalIgnoreCase)).ToList(),
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static List<MaquinaItem> CreateMachines()
    {
        return
        [
            new MaquinaItem { Maquina_id = 1, Numero = 101, NomeModelo = "CNC Alpha", IpAddress = "10.0.0.1", Estado = "DISPONIVEL", FaseDedicada_id = 1 },
            new MaquinaItem { Maquina_id = 2, Numero = 102, NomeModelo = "CNC Beta", IpAddress = "", Estado = "EM_USO", FaseDedicada_id = 2 },
            new MaquinaItem { Maquina_id = 3, Numero = 103, NomeModelo = "CNC Gamma", IpAddress = "10.0.0.3", Estado = "MANUTENCAO", FaseDedicada_id = 3 },
            new MaquinaItem { Maquina_id = 4, Numero = 104, NomeModelo = "Fresa Delta", IpAddress = "", Estado = "DISPONIVEL", FaseDedicada_id = 1 },
            new MaquinaItem { Maquina_id = 5, Numero = 105, NomeModelo = "Torno Epsilon", IpAddress = "10.0.0.5", Estado = "EM_USO", FaseDedicada_id = 2 },
            new MaquinaItem { Maquina_id = 6, Numero = 106, NomeModelo = "Laser Zeta", IpAddress = "10.0.0.6", Estado = "MANUTENCAO", FaseDedicada_id = 3 },
            new MaquinaItem { Maquina_id = 7, Numero = 107, NomeModelo = "Laser Eta", IpAddress = "", Estado = "DISPONIVEL", FaseDedicada_id = 1 }
        ];
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://tipmolde.test/")
        };
    }

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static string CreateJwtToken(int userId)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"sub\":\"{userId}\"}}");
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
