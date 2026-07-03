namespace TipMolde.Services;

public enum AppFeature
{
    Dashboard,
    Utilizadores,
    Clientes,
    Encomendas,
    PedidosMaterial,
    Producao,
    Maquinas,
    Desenho,
    Relatorios,
    Definicoes
}

public sealed class AuthorizationService
{
    private const string AdminRole = "ADMIN";

    private static readonly HashSet<AppFeature> AllFeatures = Enum
        .GetValues<AppFeature>()
        .ToHashSet();

    private static readonly Dictionary<string, HashSet<AppFeature>> RolePermissions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [AdminRole] = new HashSet<AppFeature>(AllFeatures),
            ["GESTOR_COMERCIAL"] = new()
            {
                AppFeature.Dashboard,
                AppFeature.Clientes,
                AppFeature.Encomendas,
                AppFeature.PedidosMaterial,
                AppFeature.Definicoes
            },
            ["GESTOR_DESENHO"] = new()
            {
                AppFeature.Dashboard,
                AppFeature.Desenho,
                AppFeature.Definicoes
            },
            ["GESTOR_PRODUCAO"] = new()
            {
                AppFeature.Dashboard,
                AppFeature.Producao,
                AppFeature.Maquinas,
                AppFeature.Definicoes
            }
        };

    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;

    private int? _cachedUserId;
    private string? _cachedRole;

    public AuthorizationService(
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService)
    {
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
    }

    public async Task<string?> GetCurrentRoleAsync(bool forceRefresh = false)
    {
        var currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        if (currentUserId is null)
        {
            Clear();
            return null;
        }

        if (!forceRefresh &&
            _cachedUserId == currentUserId &&
            !string.IsNullOrWhiteSpace(_cachedRole))
        {
            return _cachedRole;
        }

        var utilizador = await _utilizadoresService.GetCurrentUserAsync();

        _cachedUserId = utilizador.User_id;
        _cachedRole = NormalizeRole(utilizador.Role);

        return _cachedRole;
    }

    public async Task<bool> CanAccessAsync(AppFeature feature, bool forceRefresh = false)
    {
        var role = await GetCurrentRoleAsync(forceRefresh);
        return IsRoleAuthorized(role, feature);
    }

    public bool CanCreateMachines() => HasAnyRole(AdminRole);

    public bool CanDeleteMachines() => HasAnyRole(AdminRole);

    public bool CanManageProductionPhases() => HasAnyRole(AdminRole);

    public bool CanEditMachineAdministrativeFields() => HasAnyRole(AdminRole);

    public bool CanEditMachineState() => HasAnyRole(AdminRole, "GESTOR_PRODUCAO");

    public bool CanManagePieces() => HasAnyRole(AdminRole, "GESTOR_DESENHO");

    public bool CanDeleteClients() => HasAnyRole(AdminRole);

    public void Clear()
    {
        _cachedUserId = null;
        _cachedRole = null;
    }

    private static bool IsRoleAuthorized(string? role, AppFeature feature)
    {
        var normalizedRole = NormalizeRole(role);
        if (string.IsNullOrWhiteSpace(normalizedRole))
            return false;

        return RolePermissions.TryGetValue(normalizedRole, out var allowedFeatures) &&
               allowedFeatures.Contains(feature);
    }

    private bool HasAnyRole(params string[] roles)
    {
        var normalizedRole = NormalizeRole(_cachedRole);
        if (string.IsNullOrWhiteSpace(normalizedRole))
            return false;

        return roles.Any(role => string.Equals(normalizedRole, NormalizeRole(role), StringComparison.Ordinal));
    }

    private static string? NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role)
            ? null
            : role.Trim().ToUpperInvariant();
}
