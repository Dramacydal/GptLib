using System.Net;
using System.Text.Json.Nodes;
using GptLib.Providers.Abstraction;

namespace GptLib.Providers;

public class LocalGpt4FreeProvider : Gpt4FreeProvider
{
    public LocalGpt4FreeProvider()
    {
        Url = "http://localhost:1337/v1/chat/completions";
    }

    protected override async Task<JsonObject> CreatePayload(Conversation conversation, string modelName,
        GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        var payload = await base.CreatePayload(conversation, modelName, settings, proxy, uploadedFileCache);
        payload["provider"] = Name;

        return payload;
    }
}
