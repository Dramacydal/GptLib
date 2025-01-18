using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace GptLib.Providers.Abstraction;

public abstract class AbstractProvider : IProvider
{
    public string Name { get; set; }

    public List<string> Models { get; set; } = new();

    public string Url { get; set; }

    public string SystemPrompt { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public Dictionary<string, string> QueryParamteres { get; set; } = new();

    public bool NeedProxy { get; set; }
    
    public bool CanUpload { get; set; }

    public override string ToString() => Name;
    
    protected virtual string ModelRole => "model";
    
    protected virtual Uri PrepareUri(string modelName)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var item in QueryParamteres)
            query.Add(item.Key, item.Value);

        var ub = new UriBuilder(Url);
        ub.Query = query.ToString();

        return ub.Uri;
    }

    protected abstract JsonObject CreatePayload(Conversation conversation, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache);

    protected abstract GptResponse ParseResponse(string stringResponse);

    public GptResponse MakeRequest(Conversation conversation, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        var client = GetClient(proxy);

        client.DefaultRequestHeaders.Add("Accept", "text/event-stream");

        foreach (var header in Headers)
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

        var json = JsonSerializer.Serialize(CreatePayload(conversation, modelName, settings, proxy, uploadedFileCache));

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // _lastRequestJson = json;

        var uri = PrepareUri(modelName);
        var result = client.PostAsync(uri, content).Result;
        var stringResponse = result.Content.ReadAsStringAsync().Result;

        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception(stringResponse);

        return ParseResponse(stringResponse);
    }

    protected HttpClient GetClient(IWebProxy? proxy)
    {
        var handler = new HttpClientHandler();
        if (proxy != null)
            handler.Proxy = proxy;

        return new HttpClient(handler);
    }

    public abstract UploadFileInfo UploadFile(string path, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache);
}
