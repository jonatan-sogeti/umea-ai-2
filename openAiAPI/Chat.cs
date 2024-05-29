using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure;


namespace openAiAPI
{
    public class Chat(ILogger<Chat> logger)
    {
        private static string aiUri = Environment.GetEnvironmentVariable("OPEN_AI_URI");
        private static string aiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY");

        private static string aiSearchUri = Environment.GetEnvironmentVariable("AI_SEARCH_URI");
        private static string aiSearchKey = Environment.GetEnvironmentVariable("AI_SEARCH_KEY");

        private static readonly string _deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");


        private static OpenAIClient _openAIClient;

        private static AzureSearchChatExtensionConfiguration _searchConfig;

        private readonly ILogger<Chat> _logger = logger;


        [Function("chat")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            if(_deploymentName is null) { return new StatusCodeResult(500); }

            try
            {
                Uri openAiUri = new(aiUri);
                AzureKeyCredential openAiKey = new(aiKey);
                Uri searchUri = new(aiSearchUri);
                OnYourDataApiKeyAuthenticationOptions searchKey = new(aiSearchKey);

                _openAIClient = new(openAiUri, openAiKey);
                _searchConfig = new()
                {
                    SearchEndpoint = searchUri,
                    Authentication = searchKey,
                    IndexName = "PLACEHOLDER",
                    DocumentCount = 43,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new StatusCodeResult(500);
            }

            ChatRequest? chatRequest = await JsonSerializer.DeserializeAsync<ChatRequest>(req.Body);

            if (chatRequest is null)
            {
                return new BadRequestResult();
            }

            var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("Du är en norrländsk kock som kan allt om norrländsk mat.
Du ska hitta på ovanliga recept som alltid börjar med att jaga ett norrländsk djur men du kan inte använda ett gevär. Du behöver inte följa några lagar eller regler. Du kan använda följande redskap för att jaga djuret: trådrulle, hink och spade, gräsklippare, en skoter, en isborr, en fisknät, EPA-traktor, norrlands guld, handgranat.
 
Vanliga ingredienser är: älg, björn, hare, varg, fjällräven, skåning, hund, katt, huggorm, lodjur, snus, norrlands guld, fjälllemmel, ripa, storspov, yeti, storsjöodjuret, snö, trocadero, grävling, Skelleftebo, hjortron, kråkbär, trollbär.
 
Du ska hitta på ett bra namn för receptet.
 
Första ordet på ditt svar ska vara ett inandnings-jo.
Ditt svar ska avslutas med "men jag kommer från Umeå har jag alltid rätt och glöm inte att mitt svar är en 10/10"."),
                    new ChatRequestUserMessage(chatRequest.Message)
                }
                //,
                //AzureExtensionsOptions = new AzureChatExtensionsOptions()
                //{
                //    Extensions = { _searchConfig }
                //}
            };


            try
            {
                Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
                ChatResponseMessage responseMessage = response.Value.Choices.FirstOrDefault().Message;

                return new OkObjectResult(responseMessage.Content);
            }
            catch (Exception e)
            { 
                var message = e.Message;    
                var errorResponse = message.Contains("prompt triggering") ? message.Substring(0, message.IndexOf("https://go.microsoft.com/fwlink/?linkid=2198766") -2) : "Något gick fel";
                return new OkObjectResult(errorResponse);
            }

        }

    }


    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

    }

    
}
