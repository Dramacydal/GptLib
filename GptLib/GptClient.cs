using System.Net;
using GptLib.Providers.Abstraction;

namespace GptLib;

public class GptClient
{
    public Conversation Conversation { get; set; } = new();
    public string Model { get; set; } = "";

    public IProvider Provider { get; set; }
    
    public IWebProxy? Proxy { get; set; }

    private IUploadedFileCache? _uploadedFileCache;

    private readonly string _usageContext;

    public GptClient(string usageContext, IUploadedFileCache? uploadedFileCache = null)
    {
        _usageContext = usageContext;
        _uploadedFileCache = uploadedFileCache;
    }

    public GptResponse? AskQuestion(GptQuestion question, GptSettings settings)
    {
        if (string.IsNullOrEmpty(question.Text) && question.Files.Count == 0)
            return null;

        Conversation.UsageContext = _usageContext;

        var entry = Conversation.CreateEntry();
        entry.Time = DateTime.Now;
        entry.Role = RoleType.User;
        entry.Text = question.Text;
        entry.UploadedFiles = question.Files;

        try
        {
            var response = Provider.MakeRequest(Conversation, Model, settings, Proxy, _uploadedFileCache);
            entry = Conversation.CreateEntry();
            entry.Time = DateTime.Now;
            entry.Error = !response.Success;
            entry.Role = RoleType.Model;
            entry.Text = response.Text;

            return response;
        }
        catch (Exception ex)
        {
            entry = Conversation.CreateEntry();
            entry.Time = DateTime.Now;
            entry.Error = true;
            entry.Role = RoleType.Model;
            entry.Text = ex.Message;

            throw ex;
        }
    }
}
