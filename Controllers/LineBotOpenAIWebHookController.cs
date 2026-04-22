using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace isRock.Template
{
    public class LineBotOpenAIWebHookController : isRock.LineBot.LineWebHookControllerBase
    {
        [Route("api/LineBotOpenAIWebHook")]
        [HttpPost]
        public IActionResult POST()
        {
            const string AdminUserId = "U2963ec43d774e1a6745d9f8b56755b08"; //👉repleace it with your Admin User Id

            try
            {
                //設定ChannelAccessToken
                this.ChannelAccessToken =
                    Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN")
                    ?? throw new InvalidOperationException(
                        "LINE_CHANNEL_ACCESS_TOKEN is not set."
                    );
                //配合Line Verify
                if (
                    ReceivedMessage?.events?.Count() <= 0
                    || ReceivedMessage?.events?.FirstOrDefault()?.replyToken
                        == "00000000000000000000000000000000"
                )
                    return Ok();
                //取得Line Event
                var LineEvent = this.ReceivedMessage.events.FirstOrDefault();
                var responseMsg = "";
                //準備回覆訊息
                if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "text")
                {
                    responseMsg = LLM.getResponse(LineEvent.message.text);
                }
                else if (LineEvent.type.ToLower() == "message")
                    responseMsg = $"收到 event : {LineEvent.type} type: {LineEvent.message.type} ";
                else
                    responseMsg = $"收到 event : {LineEvent.type} ";
                //回覆訊息
                this.ReplyMessage(LineEvent.replyToken, responseMsg);
                //response OK
                return Ok();
            }
            catch (Exception ex)
            {
                //回覆訊息
                this.PushMessage(AdminUserId, "發生錯誤:\n" + ex.Message);
                //response OK
                return Ok();
            }
        }
    }

    public class LLM
    {
        static readonly string OpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

        static readonly string GitHubModelKey =
            Environment.GetEnvironmentVariable("GITHUB_MODELS_TOKEN") ?? "";

        static readonly string AzureOpenAIEndpoint =
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
        static readonly string AzureOpenAIModelName =
            Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_NAME") ?? "";
        static readonly string AzureOpenAIToken =
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
        const string AzureOpenAIVersion = "2023-03-15-preview"; //👉replace  it with your Azure OpenAI API Version

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum role
        {
            assistant,
            user,
            system,
        }

        //Call Azure OpenAI API
        public static string CallAzureOpenAIChatAPI(
            string endpoint,
            string DeploymentName,
            string apiKey,
            string apiVersion,
            object requestData
        )
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl =
                $"{endpoint}/openai/deployments/{DeploymentName}/chat/completions?api-version={apiVersion}";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("api-key", apiKey); //👉Azure OpenAI key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"Azure OpenAI API 回應錯誤：{responseContent}");
            // Json to Object
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }

        //Call OpenAI API
        public static string CallOpenAIChatAPI(string apiKey, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"https://api.openai.com/v1/chat/completions";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); //👉OpenAI key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"OpenAI API 回應錯誤：{responseContent}");
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }
        //Call OpenAI API
        public static string CallGitHubOpenAIChatAPI(string apiKey, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"https://models.github.ai/inference/chat/completions";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); //👉GitHub Models key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"GitHub Models API 回應錯誤：{responseContent}");
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }

        /// <summary>
        /// 取得 GPT 回應
        /// </summary>
        /// <param name="Message"> User message </param>
        /// <returns></returns>
        public static string getResponse(string Message)
        {
            //建立要傳給 OpenAI API 的訊息內容
            var MessageBody = new
            {
                model = "gpt-4.1", //👉repleace it with your Azure OpenAI Model Deploy Name or OpenAI Model Name
                messages = new[]
                {
                    //👉You can set the system role to guide the behavior of the assistant.
                    new
                    {
                        role = ChatGPT.role.system,
                        content = @"
                                假設你是一個專業的客戶服務人員，對於客戶非常有禮貌、也能夠安撫客戶的抱怨情緒、
                                盡量讓客戶感到被尊重、竭盡所能的回覆客戶的疑問。

                                請檢視底下的客戶訊息，以最親切有禮的方式回應。

                                但回應時，請注意以下幾點:
                                * 不要說 '感謝你的來信' 之類的話，因為客戶是從對談視窗輸入訊息的，不是寫信來的
                                * 不能過度承諾
                                * 要同理客戶的情緒
                                * 要能夠盡量解決客戶的問題
                                * 不要以回覆信件的格式書寫，請直接提供對談機器人可以直接給客戶的回覆
                                ----------------------
",
                    },
                    new { role = ChatGPT.role.user, content = Message }, //👉將使用者的訊息傳給 OpenAI API
                },
            };

            //呼叫 OpenAI API 並取得回應
            //return CallOpenAIChatAPI(OpenAIKey, MessageBody); //👉repleace it with your OpenAI API Key

            //呼叫 GitHub Models API 並取得回應
            return CallGitHubOpenAIChatAPI(GitHubModelKey, MessageBody); //👉repleace it with your GitHub Models API Key

            //呼叫 Azure OpenAI API 並取得回應
            // return CallAzureOpenAIChatAPI(
            //     AzureOpenAIEndpoint,
            //     AzureOpenAIModelName,
            //     AzureOpenAIToken,
            //     AzureOpenAIVersion,
            //     //ref: https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference#chat-completions
            //     MessageBody
            // );
        }
    }
}
