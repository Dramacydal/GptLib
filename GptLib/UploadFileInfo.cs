namespace GptLib;

public class UploadFileInfo
{
    public string ProviderName { get; set; }

    public string DisplayName { get; set; }

    public string FilePath { get; set; }

    public string MimeType { get; set; }

    public string Uri { get; set; }
    
    public string UploadedName { get; set; }

    public long Size { get; set; }

    public DateTime ModifyDate { get; set; }

    public DateTime ExpirationDate { get; set; }
}
