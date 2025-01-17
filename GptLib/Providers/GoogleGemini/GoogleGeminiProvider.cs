using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using GptLib.Providers.Abstraction;

namespace GptLib.Providers.GoogleGemini;

public class GoogleGeminiProvider : AbstractProvider
{
    public const string ModelGemini15Pro = "gemini-1.5-pro"; 
    public const string ModelGemini15Flash = "gemini-1.5-flash"; 
    public const string ModelGemini15Flash8b = "gemini-1.5-flash-8b"; 
    public const string ModelGemini20FlashExp = "gemini-2.0-flash-exp"; 
    public const string ModelGeminiExp1206 = "gemini-exp-1206";
    public const string ModelGemini20ThinkingExp = "gemini-2.0-flash-thinking-exp-1219"; 
    public const string ModelLearnMl15ProExp = "learnlm-1.5-pro-experimental";

    public GoogleGeminiProvider(string key, IWebProxy proxy)
    {
        Name = "Google Gemini Official";
        Models =
        [
            ModelGemini15Pro,
            ModelGemini15Flash,
            ModelGemini15Flash8b,
            ModelGemini20FlashExp,
            ModelGeminiExp1206,
            ModelGemini20ThinkingExp,
            ModelLearnMl15ProExp,
        ];
        Url = "https://generativelanguage.googleapis.com/v1beta/models/{model_name}:generateContent";
        QueryParamteres = new()
        {
            ["key"] = key,
        };
        NeedProxy = true;
        CanUpload = true;
        Proxy = proxy;
    }

    protected override Uri PrepareUri(string modelName)
    {
        return new Uri(base.PrepareUri(modelName).ToString().Replace("{model_name}", modelName));
    }

    protected override JsonObject CreatePayload(Conversation conversation, string modelName, GptSettings settings,
        IUploadedFileCache? uploadedFileCache)
    {
        JsonObject obj = new();

        var history = new JsonArray();
        foreach (var entry in conversation.History)
        {
            if (entry.Error)
                continue;

            var parts = new List<object>()
            {
                new
                {
                    text = entry.Question,
                }
            };

            foreach (var file in entry.UploadedFiles)
            {
                var uploadedFile = UploadFile(file, uploadedFileCache);

                parts.Add(new
                {
                    file_data = new
                    {
                        mime_type = uploadedFile.MimeType,
                        file_uri = uploadedFile.Uri,
                    }
                });
            }

            // if (entry.FileData != null)
            // {
            //     parts.Add(new
            //     {
            //         
            //     });
            // }
            
            history.Add(new
            {
                role = "user",
                parts = parts
            });

            if (!string.IsNullOrEmpty(entry.Answer))
            {
                history.Add(new
                {
                    role = "model",
                    parts = new List<object>()
                    {
                        new
                        {
                            text = entry.Answer,                        
                        }
                    }
                });
            }
        }

        Settings geminiSettings = settings as Settings;
        
        if (geminiSettings.SafetySettings.Count > 0)
        {
            var safetyJson = new JsonArray();
            foreach (var (category, threshold) in geminiSettings.SafetySettings)
            {
                safetyJson.Add(new
                {
                    category = category,
                    threshold = threshold,
                });
            }
            
            obj["safetySettings"] = safetyJson;
        }

        if (geminiSettings.Temperature > 0 || !string.IsNullOrEmpty(geminiSettings.ResponseMimeType))
        {
            var generationConfig = new JsonObject()
            {
            };
            
            if (geminiSettings.Temperature > 0)
                generationConfig["temperature"] = geminiSettings.Temperature;
            if (!string.IsNullOrEmpty(geminiSettings.ResponseMimeType))
                generationConfig["responseMimeType"] = geminiSettings.ResponseMimeType;
            
            obj["generationConfig"] = generationConfig;
        }

        if (geminiSettings.Instructions.Count > 0)
        {
            JsonObject instructions = new()
            {
                ["role"] = this.SystemRole,
            };

            JsonArray parts = new();

            foreach (var line in geminiSettings.Instructions)
            {
                parts.Add(new
                {
                    text = line
                    // inlineData
                    // fileData
                });
            }

            // instructions["role"] = null;
            instructions["parts"] = parts;
            obj["systemInstruction"] = instructions;
        }

        obj["contents"] = history;

        return obj;
    }

    protected override GptResponse ParseResponse(string stringResponse)
    {
        var obj = JsonObject.Parse(stringResponse);
        if (obj["error"] != null)
        {
            return new()
            {
                Success = false,
                Text = obj["error"]["message"].ToString(),
            };
        }

        if (obj["candidates"] == null)
        {
            return new()
            {
                Success = false,
                Text = "Candidate field not found",
            };
        }

        var text = "";
        foreach (var candidate in obj["candidates"].AsArray())
        {
            if (candidate["finish_reason"]?.ToString() == "finishReason")
            {
                return new()
                {
                    Success = false,
                    Text = stringResponse,
                };
            }
            
            foreach (var part in candidate["content"]["parts"].AsArray())
            {
                text += part["text"];
            }
        }

        return new()
        {
            Success = true,
            Text = text,
        };
    }

    public override UploadFileInfo UploadFile(string filePath, IUploadedFileCache? uploadedFileCache)
    {
        var client = GetClient();

        var info = new FileInfo(filePath);

        var mime = MimeKit.MimeTypes.GetMimeType(filePath);

        UploadFileInfo file = new()
        {
            ProviderName = Name,
            DisplayName = info.Name,
            FilePath = Path.GetFullPath(filePath),
            MimeType = mime,
            Size = info.Length,
            ModifyDate = info.LastWriteTime,
        };

        var oldFile = uploadedFileCache?.Load(file);
        if (oldFile != null)
            return oldFile;

        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length",
            info.Length.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", mime);

        var json = JsonSerializer.Serialize(new
        {
            file = new
            {
                display_name = info.Name,
            }
        });

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var result = client
            .PostAsync(
                "https://generativelanguage.googleapis.com/upload/v1beta/files?key=" +
                HttpUtility.HtmlEncode(QueryParamteres["key"]), content).Result;

        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception("Get file upload url failed");

        if (!result.Headers.TryGetValues("x-goog-upload-url", out var uploadUrls))
            throw new Exception("Failed to get upload url header");

        client.DefaultRequestHeaders.Clear();

        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Length", info.Length.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");

        var content2 = new ByteArrayContent(File.ReadAllBytes(filePath));

        result = client.PostAsync(uploadUrls.First(), content2).Result;
        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception("Failed to upload file");

        var textData = result.Content.ReadAsStringAsync().Result;

        var jsonObj = JsonObject.Parse(textData);

        var uploadedName = jsonObj["file"]["name"].ToString();
        file.UploadedName = uploadedName.Substring(uploadedName.IndexOf("/") + 1);
        file.Uri = jsonObj["file"]["uri"].ToString();
        file.ExpirationDate = jsonObj["file"]["expirationTime"].GetValue<DateTime>();

        uploadedFileCache?.Store(file);

        return file;
    }
}