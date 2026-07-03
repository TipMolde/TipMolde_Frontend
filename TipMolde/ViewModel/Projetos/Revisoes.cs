using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class ProjetoDetalheViewModel
{
    [RelayCommand]
    private async Task CriarRevisaoAsync()
    {
        if (!IsAdmin || Projeto is null || Projeto.Projeto_id <= 0 || isCreatingRevisao)
            return;

        if (!CanCreateRevisao)
        {
            await _dialogService.ShowInfoAsync(
                "Revisao bloqueada",
                GetBloqueioCriacaoRevisaoMensagem());
            return;
        }

        var descricaoAlteracoes = await _dialogService.PromptAsync(
            $"Nova revisao para {Projeto.NomeProjetoDisplay}",
            "Descreve as alteracoes a validar com o cliente.",
            new PromptDialogOptions
            {
                Accept = "Criar",
                Cancel = "Cancelar",
                Placeholder = "Descreve as alteracoes",
                MaxLength = 2000,
                Keyboard = Keyboard.Text
            });

        if (string.IsNullOrWhiteSpace(descricaoAlteracoes))
            return;

        try
        {
            IsCreatingRevisao = true;
            var created = await _revisoesService.CreateAsync(Projeto.Projeto_id, descricaoAlteracoes.Trim());

            await _dialogService.ShowSuccessAsync(
                "Revisao criada",
                $"A revisao {created?.NumRevisaoDisplay ?? "nova"} foi criada com sucesso.");

            await LoadAsync(Projeto.Projeto_id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Revisao", ex.Message);
        }
        finally
        {
            IsCreatingRevisao = false;
        }
    }

    [RelayCommand]
    private async Task ResponderRevisaoAsync(RevisaoDto? revisao)
    {
        if (!IsAdmin || revisao is null || revisao.Revisao_id <= 0 || isSendingResposta)
            return;

        if (revisao.Aprovado.HasValue)
        {
            await _dialogService.ShowInfoAsync(
                "Revisao ja respondida",
                "Esta revisao ja tem resposta registada e nao pode ser alterada.");
            return;
        }

        var decisao = await _dialogService.ShowSelectionAsync(
            $"Responder a {revisao.NumRevisaoDisplay}",
            "Cancelar",
            "Aprovar",
            "Rejeitar");

        if (string.IsNullOrWhiteSpace(decisao))
            return;

        var aprovado = string.Equals(decisao, "Aprovar", StringComparison.Ordinal);
        string? feedbackTexto = null;
        string? attachmentPath = null;

        if (!aprovado)
        {
            feedbackTexto = await _dialogService.PromptAsync(
                $"Feedback da {revisao.NumRevisaoDisplay}",
                "Indica o motivo da rejeicao.",
                new PromptDialogOptions
                {
                    Accept = "Guardar",
                    Cancel = "Cancelar",
                    Placeholder = "Feedback do cliente",
                    MaxLength = 4000,
                    Keyboard = Keyboard.Text
                });

            if (string.IsNullOrWhiteSpace(feedbackTexto))
                return;

            var adicionarAnexo = await _dialogService.ShowSelectionAsync(
                "Anexo da revisao",
                "Sem anexo",
                "Anexar ficheiro");

            if (string.Equals(adicionarAnexo, "Anexar ficheiro", StringComparison.Ordinal))
                attachmentPath = await SelecionarAnexoAsync();
        }

        try
        {
            IsSendingResposta = true;
            if (string.IsNullOrWhiteSpace(attachmentPath))
            {
                await _revisoesService.UpdateRespostaClienteAsync(
                    revisao.Revisao_id,
                    aprovado,
                    feedbackTexto?.Trim(),
                    null);
            }
            else
            {
                await _revisoesService.UpdateRespostaClienteComAnexoAsync(
                    revisao.Revisao_id,
                    aprovado,
                    feedbackTexto?.Trim(),
                    attachmentPath);
            }

            await _dialogService.ShowSuccessAsync(
                "Resposta registada",
                $"A resposta da revisao {revisao.NumRevisaoDisplay} foi guardada.");

            await LoadAsync(ProjetoId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Resposta da revisao", ex.Message);
        }
        finally
        {
            IsSendingResposta = false;
        }
    }

    [RelayCommand]
    private async Task AbrirAnexoAsync(RevisaoDto? revisao)
    {
        if (revisao is null || string.IsNullOrWhiteSpace(revisao.FeedbackImagemPath))
            return;

        try
        {
            var localPath = await _revisoesService.DownloadAnexoAsync(revisao.FeedbackImagemPath);
            await _dialogService.ShowSuccessAsync(
                "Anexo descarregado",
                $"O ficheiro foi guardado em {localPath}.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Anexo da revisao", ex.Message);
        }
    }

    private async Task<string?> SelecionarAnexoAsync()
    {
        try
        {
            var file = await _filePickerService.PickAsync(new PickOptions
            {
                PickerTitle = "Seleciona imagem ou documento para a revisao",
                FileTypes = RevisaoAnexoFileTypes
            });

            return file?.FullPath;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasOpenOrApprovedRevision(ProjetoComRevisoesDto? projeto)
    {
        var ultimaRevisao = projeto?.Revisoes
            .OrderByDescending(item => item.NumRevisao)
            .FirstOrDefault();

        return ultimaRevisao is not null
            && (!ultimaRevisao.DataResposta.HasValue || ultimaRevisao.Aprovado == true);
    }

    private string GetBloqueioCriacaoRevisaoMensagem()
    {
        if (Projeto is null)
            return "Nao foi possivel carregar o projeto.";

        var ultimaRevisao = Projeto.Revisoes
            .OrderByDescending(item => item.NumRevisao)
            .FirstOrDefault();

        if (ultimaRevisao is null)
            return "Nao existe historico de revisoes suficiente para criar uma nova revisao.";

        if (!ultimaRevisao.DataResposta.HasValue)
            return "Ja existe uma revisao em aberto para este projeto.";

        if (ultimaRevisao.Aprovado == true)
            return "O cliente ja aprovou a ultima revisao, por isso nao e possivel criar outra.";

        return "Nao e possivel criar uma nova revisao neste momento.";
    }

    private static readonly FilePickerFileType RevisaoAnexoFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = [".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg"],
        [DevicePlatform.Android] = ["application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "image/png", "image/jpeg"],
        [DevicePlatform.iOS] = ["com.adobe.pdf", "org.openxmlformats.wordprocessingml.document", "public.jpeg", "public.png"],
        [DevicePlatform.MacCatalyst] = ["com.adobe.pdf", "org.openxmlformats.wordprocessingml.document", "public.jpeg", "public.png"]
    });
}
