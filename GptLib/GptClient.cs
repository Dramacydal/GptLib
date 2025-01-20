using System.Net;
using GptLib.Exceptions;
using GptLib.Providers.Abstraction;

namespace GptLib;

public class GptClient
{
    public IProvider Provider { get; set; }
    
    public string ModelName { get; set; } = "";
    
    public IWebProxy? Proxy { get; set; }

    private IUploadedFileCache? _uploadedFileCache;

    private readonly string _usageContext;

    public GptClient(string usageContext, IUploadedFileCache? uploadedFileCache = null)
    {
        _usageContext = usageContext;
        _uploadedFileCache = uploadedFileCache;
    }

    public async Task<GptResponse?> AskQuestion(GptQuestion question, History history, GptSettings settings)
    {
        if (string.IsNullOrEmpty(question.Text) && question.Files.Count == 0)
            return null;

        var questionEntry = history.CreateEntry();
        questionEntry.Time = DateTime.Now;
        questionEntry.Role = RoleType.User;
        questionEntry.Text = question.Text;
        questionEntry.Tag = question.Tag;
        questionEntry.UploadedFiles = question.Files;

        if (Provider == null)
            throw new GptException("Provider not set");
        
        var res = await Provider.MakeRequest(history, ModelName, settings, Proxy, _uploadedFileCache);

        res.Question = questionEntry;

        return res;
    }
}
