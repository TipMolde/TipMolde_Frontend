using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class AdicionarClienteViewModel : ObservableObject
{
    private readonly ClientesService _clientesService;
    private readonly IDialogService _dialogService;

    public AdicionarClienteViewModel(
        ClientesService clientesService,
        IDialogService dialogService)
    {
        _clientesService = clientesService;
        _dialogService = dialogService;
    }

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string nif = string.Empty;

    [ObservableProperty]
    private string sigla = string.Empty;

    [ObservableProperty]
    private string pais = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string telefone = string.Empty;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private static async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Create()
    {
        if (IsSaving)
            return;

        ErrorMessage = string.Empty;

        var nomeNormalizado = Nome.Trim();
        var nifNormalizado = Nif.Trim();
        var siglaNormalizada = Sigla.Trim();
        var paisNormalizado = ClienteFormDefaults.NormalizeOptional(Pais);
        var emailNormalizado = ClienteFormDefaults.NormalizeOptional(Email);
        var telefoneNormalizado = ClienteFormDefaults.NormalizeOptional(Telefone);

        ErrorMessage =
            ClienteFormDefaults.ValidateNome(nomeNormalizado) ??
            ClienteFormDefaults.ValidateNif(nifNormalizado) ??
            ClienteFormDefaults.ValidateSigla(siglaNormalizada) ??
            ClienteFormDefaults.ValidatePais(paisNormalizado) ??
            ClienteFormDefaults.ValidateEmail(emailNormalizado) ??
            ClienteFormDefaults.ValidateTelefone(telefoneNormalizado) ??
            string.Empty;

        if (!string.IsNullOrWhiteSpace(ErrorMessage))
            return;

        IsSaving = true;

        try
        {
            await _clientesService.CreateAsync(
                nomeNormalizado,
                nifNormalizado,
                siglaNormalizada,
                paisNormalizado,
                emailNormalizado,
                telefoneNormalizado);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O cliente {nomeNormalizado} foi criado com sucesso.");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
