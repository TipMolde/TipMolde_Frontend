using System.Diagnostics;

namespace TipMolde.Diagnostics;

internal static class TaskMonitor
{
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

    public static void ReportException(string context, Exception exception)
    {
        var message = $"[{DateTimeOffset.Now:O}] {context}{Environment.NewLine}{exception}";
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
