namespace TipMolde.Services;

/// <summary>
/// Enumera as areas funcionais cujo acesso e controlado no frontend.
/// </summary>
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

/// <summary>
/// Resolve a role atual do utilizador e aplica as regras de permissao da app.
/// </summary>
/// <remarks>
/// Mantem cache por utilizador autenticado para evitar chamadas repetidas
/// ao backend sempre que a navegacao ou a UI precisam de validar acesso.
/// </remarks>
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

    /// <summary>
    /// Construtor do servico de autorizacao do frontend.
    /// </summary>
    /// <param name="sessaoPersistidaService">Servico que expoe o utilizador autenticado a partir da sessao ativa.</param>
    /// <param name="utilizadoresService">Servico usado para obter a role atualizada no backend.</param>
    public AuthorizationService(
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService)
    {
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
    }

    /// <summary>
    /// Obtem a role atual do utilizador autenticado.
    /// </summary>
    /// <param name="forceRefresh">Indica se deve ignorar o cache local e voltar a consultar o backend.</param>
    /// <returns>Role normalizada do utilizador ou nulo quando nao existe sessao valida.</returns>
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

    /// <summary>
    /// Verifica se a role atual pode aceder a uma area funcional da app.
    /// </summary>
    /// <param name="feature">Feature cuja autorizacao deve ser validada.</param>
    /// <param name="forceRefresh">Indica se a role deve ser recarregada do backend antes da validacao.</param>
    /// <returns>True quando o utilizador pode aceder a feature; false caso contrario.</returns>
    public async Task<bool> CanAccessAsync(AppFeature feature, bool forceRefresh = false)
    {
        var role = await GetCurrentRoleAsync(forceRefresh);
        return IsRoleAuthorized(role, feature);
    }

    /// <summary>
    /// Indica se a role atual pode criar maquinas.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanCreateMachines() => HasAnyRole(AdminRole);

    /// <summary>
    /// Indica se a role atual pode remover maquinas.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanDeleteMachines() => HasAnyRole(AdminRole);

    /// <summary>
    /// Indica se a role atual pode gerir fases de producao.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanManageProductionPhases() => HasAnyRole(AdminRole);

    /// <summary>
    /// Indica se a role atual pode editar campos administrativos de maquinas.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanEditMachineAdministrativeFields() => HasAnyRole(AdminRole);

    /// <summary>
    /// Indica se a role atual pode alterar o estado operacional de maquinas.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanEditMachineState() => HasAnyRole(AdminRole, "GESTOR_PRODUCAO");

    /// <summary>
    /// Indica se a role atual pode gerir pecas no contexto de desenho.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanManagePieces() => HasAnyRole(AdminRole, "GESTOR_DESENHO");

    /// <summary>
    /// Indica se a role atual pode remover clientes.
    /// </summary>
    /// <returns>True quando a operacao esta autorizada.</returns>
    public bool CanDeleteClients() => HasAnyRole(AdminRole);

    /// <summary>
    /// Limpa o cache da role e do utilizador autenticado.
    /// </summary>
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
