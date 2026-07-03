namespace TipMolde.Services;

public interface IFilePickerService
{
    Task<FileResult?> PickAsync(PickOptions options);
}
