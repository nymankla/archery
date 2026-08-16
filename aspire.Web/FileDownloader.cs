using Microsoft.JSInterop;

namespace aspire.Web;

/// <summary>Saves a downloaded file to the browser via JS interop (Blazor Server has no direct client filesystem access).</summary>
public class FileDownloader(IJSRuntime js)
{
    public Task SaveAsync(ExportedFile file)
        => js.InvokeVoidAsync("fileDownload.saveFromBase64", file.FileName, file.ContentType, Convert.ToBase64String(file.Content)).AsTask();
}
