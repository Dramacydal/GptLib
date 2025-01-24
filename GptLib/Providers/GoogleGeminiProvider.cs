using System.Net;
using System.Text.Json.Nodes;
using System.Web;
using GptLib.Exceptions;
using GptLib.Providers.Abstraction;
using GptLib.Providers.GoogleGemini;

namespace GptLib.Providers;

public class GoogleGeminiProvider : AbstractProvider
{
    public const string ModelGemini15Pro = "gemini-1.5-pro"; 
    public const string ModelGemini15Flash = "gemini-1.5-flash"; 
    public const string ModelGemini15Flash8b = "gemini-1.5-flash-8b"; 
    public const string ModelGemini20FlashExp = "gemini-2.0-flash-exp"; 
    public const string ModelGeminiExp1206 = "gemini-exp-1206";
    public const string ModelGemini20ThinkingExp = "gemini-2.0-flash-thinking-exp-1219"; 
    public const string ModelLearnMl15ProExp = "learnlm-1.5-pro-experimental";

    public GoogleGeminiProvider(string key)
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
    }

    protected override Uri PrepareUri(string modelName)
    {
        return new Uri(base.PrepareUri(modelName).ToString().Replace("{model_name}", modelName));
    }

    protected override async Task<JsonObject> CreatePayload(History history, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        JsonObject obj = new();
        var historyObj = new JsonArray();
        foreach (var entry in history.Contents)
        {
            if (entry.Error)
                continue;

            var parts = new List<object>()
            {
                new
                {
                    text = entry.Text,
                }
            };

            foreach (var file in entry.UploadedFiles)
            {
                var uploadedFile = await UploadFile(file, proxy, uploadedFileCache);

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
            
            historyObj.Add(new
            {
                role = entry.Role == RoleType.User ? "user" : "model",
                parts = parts
            });
        }

        var geminiSettings = settings as Settings;
        
        if (geminiSettings?.SafetySettings?.Count > 0)
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

        if (settings.Temperature > 0 || !string.IsNullOrEmpty(geminiSettings?.ResponseMimeType))
        {
            var generationConfig = new JsonObject();
            
            if (settings.Temperature > 0)
                generationConfig["temperature"] = settings.Temperature;
            if (!string.IsNullOrEmpty(geminiSettings?.ResponseMimeType))
                generationConfig["responseMimeType"] = geminiSettings.ResponseMimeType;
            
            obj["generationConfig"] = generationConfig;
        }

        if (geminiSettings?.Instructions.Count > 0)
        {
            JsonObject instructions = new();

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

        obj["contents"] = historyObj;

        return obj;
    }

    protected override async Task<GptResponse> ParseResponse(Stream stream)
    {
        var obj = await JsonNode.ParseAsync(stream);
        if (obj["error"] != null)
        {
            return new()
            {
                Success = false,
                Answer = new()
                {
                    Error = true,
                    Text = obj["error"]["message"].ToString(),
                    Role = RoleType.Model,
                }
            };
        }

        if (obj["candidates"] == null)
        {
            return new()
            {
                Success = false,
                Answer = new()
                {
                    Error = true,
                    Text = "Candidate field not found",
                    Role = RoleType.Model,
                }
            };
        }

        var text = "";
        foreach (var candidate in obj["candidates"].AsArray())
        {
            var finishReason = candidate["finishReason"]?.ToString();
            if (finishReason == "finishReason")
            {
                return new()
                {
                    Success = false,
                    Answer = new()
                    {
                        Error = true,
                        Text = obj.ToJsonString(),
                        Role = RoleType.Model,
                    }
                };
            }

            if (finishReason == "SAFETY")
                throw new SafetyException(candidate["safetyRatings"].ToJsonString());

            foreach (var part in candidate["content"]["parts"].AsArray())
                text += part["text"];
        }

        return new()
        {
            Success = true,
            Answer = new()
            {
                Error = false,
                Text = text,
                Role = RoleType.Model,
            }
        };
    }

    public override async Task<UploadFileInfo> UploadFile(string filePath, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        var client = GetClient(proxy);

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

        if (uploadedFileCache != null)
        {
            var oldFile = await uploadedFileCache.Load(file);
            if (oldFile != null)
                return oldFile;
        }

        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length",
            info.Length.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", mime);

        var json = await JsonSerialize(new
        {
            file = new
            {
                display_name = info.Name,
            }
        });

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var result = await client
            .PostAsync(
                "https://generativelanguage.googleapis.com/upload/v1beta/files?key=" +
                HttpUtility.HtmlEncode(QueryParamteres["key"]), content);

        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception("Get file upload url failed");

        if (!result.Headers.TryGetValues("x-goog-upload-url", out var uploadUrls))
            throw new Exception("Failed to get upload url header");

        client.DefaultRequestHeaders.Clear();

        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Length", info.Length.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");

        var content2 = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));

        result = await client.PostAsync(uploadUrls.First(), content2);
        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception("Failed to upload file");

        var jsonObj = await JsonNode.ParseAsync(await result.Content.ReadAsStreamAsync());

        var uploadedName = jsonObj["file"]["name"].ToString();
        file.UploadedName = uploadedName.Substring(uploadedName.IndexOf("/") + 1);
        file.Uri = jsonObj["file"]["uri"].ToString();
        file.ExpirationDate = jsonObj["file"]["expirationTime"].GetValue<DateTime>();

        if (uploadedFileCache != null)
            await uploadedFileCache.Store(file).ConfigureAwait(false);

        return file;
    }
}
