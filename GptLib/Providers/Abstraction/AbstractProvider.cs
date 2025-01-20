using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using GptLib.Exceptions;

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

    protected abstract Task<JsonObject> CreatePayload(History history, string modelName, GptSettings settings, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache);

    protected abstract Task<GptResponse> ParseResponse(Stream stream);

    public async Task<GptResponse> MakeRequest(History history, string modelName, GptSettings settings,
        IWebProxy? proxy, IUploadedFileCache? uploadedFileCache)
    {
        if (!Models.Any(model => string.Equals(model, modelName, StringComparison.InvariantCultureIgnoreCase)))
            throw new GptException($"Provider {GetType().Name} does not support model {modelName}");
        
        var client = GetClient(proxy);

        foreach (var header in Headers)
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

        using var stream = new TextWriterTraceListener();

        var json = await JsonSerialize(await CreatePayload(history, modelName, settings, proxy, uploadedFileCache));

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var uri = PrepareUri(modelName);
        var result = await client.PostAsync(uri, content);

        if (result.StatusCode != HttpStatusCode.OK)
            throw new Exception(await result.Content.ReadAsStringAsync());

        return await ParseResponse(await result.Content.ReadAsStreamAsync());
    }

    protected async Task<string> JsonSerialize(object obj)
    {
        using MemoryStream memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, obj, obj.GetType());
        memoryStream.Position = 0;
     
        using StreamReader streamReader = new StreamReader(memoryStream);
        return await streamReader.ReadToEndAsync();
    }

    protected HttpClient GetClient(IWebProxy? proxy)
    {
        var handler = new HttpClientHandler();
        if (proxy != null)
            handler.Proxy = proxy;

        return new HttpClient(handler);
    }

    public abstract Task<UploadFileInfo> UploadFile(string path, IWebProxy? proxy, IUploadedFileCache? uploadedFileCache);
}
