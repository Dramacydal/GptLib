namespace GptLib;

public interface IUploadedFileCache
{
    public UploadFileInfo? Load(UploadFileInfo fileInfo);

    void Store(UploadFileInfo file);
}
