namespace GptLib;

public interface IUploadedFileCache
{
    public Task<UploadFileInfo> Load(UploadFileInfo fileInfo);

    public Task Store(UploadFileInfo file);
}
