using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class MaquinasService : ApiServiceBase
{
    public MaquinasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<MaquinaItem>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/Maquina?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MaquinaItem>>(response);
    }

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
