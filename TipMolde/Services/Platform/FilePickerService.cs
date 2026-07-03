namespace TipMolde.Services;

public sealed class FilePickerService : IFilePickerService
{
    public Task<FileResult?> PickAsync(PickOptions options)
    {
        return FilePicker.Default.PickAsync(options);
    }
}
