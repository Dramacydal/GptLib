using System.Text.Json.Nodes;
using GptLib.Providers.Abstraction;

namespace GptLib.Providers;

public class LocalGpt4FreeProvider : Gpt4FreeProvider
{
    public LocalGpt4FreeProvider()
    {
        Url = "http://localhost:1337/v1/chat/completions";
    }

    protected override JsonObject CreatePayload(Conversation conversation, string modelName, GptSettings settings,
        IUploadedFileCache? uploadedFileCache)
    {
        var payload = base.CreatePayload(conversation, modelName, settings, uploadedFileCache);
        payload["provider"] = Name;

        return payload;
    }
}
