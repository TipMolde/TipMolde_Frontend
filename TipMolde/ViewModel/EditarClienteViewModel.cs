using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class EditarClienteViewModel : ObservableObject
{
    private readonly ClientesService _clientesService;
    private readonly IDialogService _dialogService;

    public EditarClienteViewModel(
        ClientesService clientesService,
        IDialogService dialogService)
    {
        _clientesService = clientesService;
        _dialogService = dialogService;
    }

    [ObservableProperty]
    private int cliente_id;

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
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    public async Task LoadAsync(int clienteId)
    {
        Cliente_id = clienteId;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var cliente = await _clientesService.GetByIdAsync(clienteId);

            if (cliente is null)
            {
                ErrorMessage = "Nao foi possivel carregar o cliente para edicao.";
                return;
            }

            Nome = cliente.Nome ?? string.Empty;
            Nif = cliente.NIF ?? string.Empty;
            Sigla = cliente.Sigla ?? string.Empty;
            Pais = cliente.Pais ?? string.Empty;
            Email = cliente.Email ?? string.Empty;
            Telefone = cliente.Telefone ?? string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Save()
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
            await _clientesService.UpdateAsync(
                Cliente_id,
                nomeNormalizado,
                nifNormalizado,
                siglaNormalizada,
                paisNormalizado,
                emailNormalizado,
                telefoneNormalizado);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O cliente {nomeNormalizado} foi atualizado com sucesso.");

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
