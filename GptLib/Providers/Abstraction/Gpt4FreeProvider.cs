﻿using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GptLib.Providers.Abstraction;

public abstract class Gpt4FreeProvider : AbstractProvider
{
    protected override string ModelRole => "assistant";

    protected override JsonObject CreatePayload(Conversation conversation, string modelName, GptSettings settings,
        IUploadedFileCache? uploadedFileCache)
    {
        JsonObject obj = new()
        {
            ["conversation_id"] = conversation.Guid.ToString(),
            ["model"] = modelName,
            // ["provider"] = Name,
            ["stream"] = true,
            ["temperature"] = settings.Temperature,
        };

        var messages = new JsonArray();
        if (!string.IsNullOrEmpty(SystemPrompt))
            messages.Add(new
            {
                role = "system",
                content = SystemPrompt,
            });

        foreach (var entry in conversation.History)
        {
            if (entry.Error)
                continue;

            messages.Add(new
            {
                role = entry.Role == "system" ? ModelRole : entry.Role,
                content = entry.Question,
            });

            if (!string.IsNullOrEmpty(entry.Answer))
            {
                messages.Add(new
                {
                    role = ModelRole,
                    content = entry.Answer,
                });
            }
        }

        obj["messages"] = messages;

        return obj;
    }
    
    protected override GptResponse ParseResponse(string stringResponse)
    {
        var text = "";
        var lines = stringResponse.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var first = true;
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
                text += ParseDataLine(line);
        }

        return new GptResponse()
        {
            Success = true,
            Text = text,
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
    
    public override UploadFileInfo UploadFile(string path, IUploadedFileCache? uploadedFileCache)
    {
        throw new NotImplementedException();
    }
}