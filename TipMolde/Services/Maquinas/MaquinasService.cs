using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de maquinas no frontend.
/// </summary>
/// <remarks>
/// Disponibiliza operacoes de listagem, pesquisa, detalhe e manutencao
/// de maquinas, incluindo traducao de falhas de conectividade.
/// </remarks>
public sealed class MaquinasService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de maquinas.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public MaquinasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista maquinas de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com maquinas ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<MaquinaItem>?> GetAllAsync(int page, int pageSize)
    {
        try
        {
            using var response = await HttpClient.GetAsync($"api/Maquina?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await DeserializeAsync<PagedResult<MaquinaItem>>(response);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            throw CreateConnectivityException(ex, "Nao foi possivel contactar o backend para carregar as maquinas.");
        }
    }

    /// <summary>
    /// Obtem uma maquina pelo identificador.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina.</param>
    /// <returns>DTO da maquina ou nulo quando nao e encontrada.</returns>
    public async Task<MaquinaItem?> GetByIdAsync(int maquinaId)
    {
        using var response = await HttpClient.GetAsync($"api/Maquina/{maquinaId}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a maquina.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MaquinaItem>(response);
    }

    /// <summary>
    /// Pesquisa maquinas por termo livre.
    /// </summary>
    /// <param name="searchTerm">Termo parcial aplicado a pesquisa.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com maquinas encontradas ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<MaquinaItem>?> SearchAsync(string searchTerm, int page, int pageSize)
    {
        try
        {
            using var response = await HttpClient.GetAsync(
                $"api/Maquina/search?searchTerm={Uri.EscapeDataString(searchTerm.Trim())}&page={page}&pageSize={pageSize}");

            await ThrowIfAuthorizationFailureAsync(
                response,
                "Nao tens permissao para pesquisar maquinas.");

            if (!response.IsSuccessStatusCode)
                return null;

            return await DeserializeAsync<PagedResult<MaquinaItem>>(response);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            throw CreateConnectivityException(ex, "Nao foi possivel contactar o backend para pesquisar maquinas.");
        }
    }

    /// <summary>
    /// Cria uma nova maquina.
    /// </summary>
    /// <param name="maquinaId">Identificador tecnico da maquina.</param>
    /// <param name="numero">Numero funcional apresentado na UI.</param>
    /// <param name="nomeModelo">Modelo ou designacao da maquina.</param>
    /// <param name="ipAddress">Endereco IP associado quando aplicavel.</param>
    /// <param name="estado">Estado operacional inicial.</param>
    /// <param name="faseDedicadaId">Identificador da fase dedicada.</param>
    /// <returns>DTO da maquina criada.</returns>
    public async Task<MaquinaItem?> CreateAsync(
        int maquinaId,
        int numero,
        string nomeModelo,
        string? ipAddress,
        string estado,
        int faseDedicadaId)
    {
        var payload = new
        {
            Maquina_id = maquinaId,
            Numero = numero,
            NomeModelo = nomeModelo,
            IpAddress = ipAddress,
            Estado = estado,
            FaseDedicada_id = faseDedicadaId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/Maquina", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a maquina {numero}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<MaquinaItem>(response);
    }

    /// <summary>
    /// Atualiza parcialmente uma maquina existente.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina a atualizar.</param>
    /// <param name="numero">Novo numero funcional.</param>
    /// <param name="nomeModelo">Novo modelo ou designacao.</param>
    /// <param name="ipAddress">Novo endereco IP.</param>
    /// <param name="estado">Novo estado operacional.</param>
    /// <param name="faseDedicadaId">Nova fase dedicada.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateAsync(
        int maquinaId,
        int? numero = null,
        string? nomeModelo = null,
        string? ipAddress = null,
        string? estado = null,
        int? faseDedicadaId = null)
    {
        var payload = new
        {
            Numero = numero,
            NomeModelo = NormalizeOptional(nomeModelo),
            IpAddress = NormalizeOptional(ipAddress),
            Estado = estado,
            FaseDedicada_id = faseDedicadaId
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/Maquina/{maquinaId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a maquina {maquinaId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Remove uma maquina existente.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteAsync(int maquinaId)
    {
        using var response = await HttpClient.DeleteAsync($"api/Maquina/{maquinaId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar a maquina {maquinaId}. Estado: {(int)response.StatusCode}");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
