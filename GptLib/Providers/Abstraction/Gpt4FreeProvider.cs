using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GptLib.Providers.Abstraction;

public abstract class Gpt4FreeProvider : AbstractProvider
{
    protected override string ModelRole => "assistant";

    protected Gpt4FreeProvider()
    {
        Headers["Accept"] = "text/event-stream";
    }

    protected override async Task<JsonObject> CreatePayload(History history, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        JsonObject obj = new()
        {
            // ["conversation_id"] = conversation.Guid.ToString(),
            ["model"] = modelName,
            // ["provider"] = Name,
            ["stream"] = true,
        };

        if (settings.Temperature > 0)
            obj["temperature"] = settings.Temperature;

        var messages = new JsonArray();
        foreach (var instruction in settings.Instructions)
        {
            messages.Add(new
            {
                role = "system",
                content = instruction,
            });
        }

        foreach (var entry in history.Contents)
        {
            if (entry.Error)
                continue;

            messages.Add(new
            {
                role = entry.Role == RoleType.Model ? ModelRole : "user",
                content = entry.Text,
            });
        }

        obj["messages"] = messages;

        return obj;
    }

    protected override async Task<GptResponse> ParseResponse(Stream stream)
    {
        using var r = new StreamReader(stream);

        var text = "";
        var lines = (await r.ReadToEndAsync()).Split("\n",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
                text += ParseDataLine(line);
        }

        return new GptResponse()
        {
            Success = true,
            Answer = new()
            {
                Role = RoleType.Model,
                Text = text,
                Time = DateTime.Now,
            },
        };
    }

    private string ParseDataLine(string line)
    {
        if (!line.StartsWith("data: "))
            return "";

        if (line == "data: [DONE]")
            return "";

        var text = "";
        var newLine = line.Substring(6);

        var respJson = JsonObject.Parse(newLine);
        if (respJson["error"] != null)
            throw new Exception(respJson["error"].ToString());

        try
        {
            var msg = respJson["choices"][0]["delta"]["content"];
            if (msg == null)
                return text;

            if (msg.ToString().StartsWith("data: "))
                return ParseDataLine(msg.ToString());

            msg = Regex.Replace(msg.ToString(), "([^\r])\n", "$1\r\n");
            text += msg;

        }
        catch (Exception ex)
        {
            throw ex;
        }

        return text;
    }
    
    public override Task<UploadFileInfo> UploadFile(string path, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        throw new NotImplementedException();
    }
}