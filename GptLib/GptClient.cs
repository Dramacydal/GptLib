using GptLib.Providers.Abstraction;

namespace GptLib;

public class GptClient
{
    public Conversation Conversation { get; set; } = new();
    public string Model { get; set; } = "";
    
    public AbstractProvider Provider { get; set; }

    private IUploadedFileCache? _uploadedFileCache;

    public GptClient(IUploadedFileCache? uploadedFileCache = null)
    {
        _uploadedFileCache = uploadedFileCache;
    }
    
    public void AddInstruction(string prompt)
    {
        var entry = Conversation.CreateEntry();
        entry.Time = DateTime.Now;
        entry.Role = "system";
        entry.Question = prompt;
    }
    
    public GptResponse? AskQuestion(GptQuestion question, GptSettings settings)
    {
        if (string.IsNullOrEmpty(question.Text) && question.Files.Count == 0)
            return null;

        var entry = Conversation.CreateEntry();
        entry.Time = DateTime.Now;
        entry.Role = question.Role;
        entry.Question = question.Text;
        entry.UploadedFiles = question.Files;

        try
        {
            var response = Provider.MakeRequest(Conversation, Model, settings, _uploadedFileCache);
            entry.Error = !response.Success;
            entry.Answer = response.Text;

            return response;
        }
        catch (Exception ex)
        {
            Conversation.Last!.Answer = ex.Message;
            Conversation.Last!.Error = true;

            throw ex;
        }
    }
}
