using System.Net;

namespace GptLib.Providers.Abstraction;

public interface IProvider
{
    public Task<GptResponse> MakeRequest(History history, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache);
    
    public string Name { get; set; }
    
    public List<string> Models { get; set; }
    
    public bool CanUpload { get; set; }
}
