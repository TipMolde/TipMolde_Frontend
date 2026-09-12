using System.Diagnostics;

namespace TipMolde.Diagnostics;

/// <summary>
/// Observa tarefas fire-and-forget para evitar falhas silenciosas no frontend.
/// </summary>
/// <remarks>
/// Encapsula o tratamento minimo de excecoes assincronas que nao sao aguardadas
/// diretamente pelo ciclo de vida da UI.
/// </remarks>
internal static class TaskMonitor
{
    /// <summary>
    /// Regista observacao sobre uma tarefa potencialmente longa ou desacoplada da UI.
    /// </summary>
    /// <param name="context">Identificador funcional do ponto onde a tarefa foi iniciada.</param>
    /// <param name="task">Tarefa a observar e a reportar em caso de falha.</param>
    public static void Observe(string context, Task task)
    {
        if (task.IsCompleted)
        {
            if (task.IsFaulted && task.Exception is not null)
                ReportException(context, task.Exception.GetBaseException());

            return;
        }

        _ = ObserveCoreAsync(context, task);
    }

    /// <summary>
    /// Regista uma excecao operacional capturada fora do fluxo normal de controlo.
    /// </summary>
    /// <param name="context">Contexto funcional que ajuda a localizar a origem da falha.</param>
    /// <param name="exception">Excecao final a incluir no log diagnostico.</param>
    public static void ReportException(string context, Exception exception)
    {
        var message = $"[{DateTimeOffset.Now:O}] {context}{Environment.NewLine}{exception}";
        StartupCrashLogger.LogException(context, exception);
        Trace.TraceError(message);
        Debug.WriteLine(message);
    }

    private static async Task ObserveCoreAsync(string context, Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Lifecycle cancellations are expected and should not be reported as failures.
        }
        catch (Exception ex)
        {
            ReportException(context, ex);
        }
    }
}
