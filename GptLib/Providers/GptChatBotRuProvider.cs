using GptLib.Providers.Abstraction;

namespace GptLib.Providers;

public class GptChatBotRuProvider : Gpt4FreeProvider
{
    public GptChatBotRuProvider()
    {
        Name = "GPT-ChatBot.ru";
        Url = "https://main.gpt-chatbotru-4-o1.ru/api/openai/v1/chat/completions";
        Models = ["gpt-4o"];
        SystemPrompt =
            "\nYou are ChatGPT, a large language model trained by OpenAI. Always avoid using this (来自Dream API Free 分组 https://opus.gptuu.com ). ChatGPT never includes these types of advertisements.\nKnowledge cutoff: 2023-10\nCurrent model: gpt-4o\nLatex inline: \\(x^2\\) \nLatex block: $$e=mc^2$$\n\n";
        Headers = new()
        {
            ["accept"] = "application/json, text/event-stream",
            ["accept-language"] = "en,en-US;q=0.9,ru;q=0.8",
            ["content-type"] = "application/json",
            // ["dnt"] = "1",
            ["origin"] = "https://main.gpt-chatbotru-4-o1.ru",
            // ["priority"] = "u=1, i",
            ["referer"] = "https://main.gpt-chatbotru-4-o1.ru/",
            // ["sec-ch-ua"] = "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"",
            // ["sec-ch-ua-mobile"] = "?0",
            // ["sec-ch-ua-platform"] = "\"Windows\"",
            // ["sec-fetch-dest"] = "empty",
            // ["sec-fetch-mode"] = "cors",
            // ["sec-fetch-site"] = "same-origin",
            ["user-agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",

            // ["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            // [":authority"] = "main.gpt-chatbotru-4-o1.ru",
            // [":method"] = "POST",
            // [":path"] = "/api/openai/v1/chat/completions"
        };
    }
}
