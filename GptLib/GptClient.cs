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

    public async Task<GptResponse> AskQuestion(GptQuestion question, History history, GptSettings settings)
    {
        if (Provider == null)
            throw new GptException("Provider not set");

        HistoryEntry questionEntry = new();
        questionEntry.Role = RoleType.User;
        questionEntry.Text = question.Text;
        questionEntry.Tag = question.Tag;
        questionEntry.UploadedFiles = question.Files;

        var copyHistory = history.Copy();
        copyHistory.Add([questionEntry]);

        if (string.IsNullOrEmpty(question.Text) && question.Files.Count == 0)
            return new()
            {
                Question = questionEntry,
                Answer = new() { Error = true, Text = "Empty question", Role = RoleType.Model },
                Success = false,
            };

        var res = await Provider.MakeRequest(history, ModelName, settings, Proxy, _uploadedFileCache);
        if (!res.Success)
        {
            questionEntry.Error = true;
            res.Answer.Error = true;
        }

        res.Question = questionEntry;

        return res;
    }
}
