using Azure;
using Azure.AI.OpenAI;
using Chatbot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

const string openAiBaseUriKey = "OpenAI-BaseUri";
const string openAiApiKeyKey = "OpenAI-ApiKey";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // add services here if you want to use them through dependency injection
    })
    .Build();

var configuration = host.Services.GetRequiredService<IConfiguration>();

var openAiBaseUri = configuration[openAiBaseUriKey];
var openAiApiKey = configuration[openAiApiKeyKey];

if (string.IsNullOrEmpty(openAiBaseUri)) 
    throw new ArgumentNullException(openAiApiKeyKey, "Please provide the Open AI Base URI in the app settings file!");

if(string.IsNullOrEmpty(openAiApiKey)) 
    throw new ArgumentNullException(openAiApiKeyKey, "Please provide the Open AI API Key in the app settings file!");

// Create Open AI Client
var client = new OpenAIClient(
    new Uri(openAiBaseUri),
    new AzureKeyCredential(openAiApiKey));

// Set max number of messages to keep in context without taking tokens into account
const int maxContextMessages = 10;

ChatCompletionsOptions chatCompletionsOptions = new()
{
    DeploymentName = "gpt-35-turbo",
    Temperature = 0,
    Tools =
    {
        WeatherService.GetWeatherFunctionDefinition()
    },
    ToolChoice = ChatCompletionsToolChoice.Auto
};

// Add system message
chatCompletionsOptions.Messages.Add(
    new ChatRequestSystemMessage(
        """
        You are a personal assistant with a good sense of humor and keeping it informal.
        Only return a tool call for weather information when the user explicitly asks for the weather information!
        """));

// Start chat bot prompts
while (true)
{
    Console.Write("User: ");
    var userInput = Console.ReadLine();

    if (userInput?.ToLower() == "quit" || userInput?.ToLower() == "q")
    {
        break;
    }

    // Remove messages from the list if we reached maximum message context
    // Keep the 1st message (index = 0) as that is the system message
    while (chatCompletionsOptions.Messages.Count >= maxContextMessages)
    {
        chatCompletionsOptions.Messages.RemoveAt(1);
    }

    // Add the new user input message
    var newUserInput = new ChatRequestUserMessage(userInput);
    chatCompletionsOptions.Messages.Add(newUserInput);

    // Send the chat request to OpenAI
    var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);

    var responseMessage = "";

    // Handle Tool Call Response
    if (response.Value.Choices[0].FinishReason == CompletionsFinishReason.ToolCalls)
    {
        var toolCalls = response.Value.Choices[0].Message.ToolCalls;

        foreach (var toolCall in toolCalls)
        {
            // Get function name 
            var functionCall = toolCall as ChatCompletionsFunctionToolCall;

            // Call weather function
            if (functionCall?.Name == "GetWeather")
            {
                var parameters = JsonConvert.DeserializeObject<Weather>(functionCall.Arguments);
                var weather = WeatherService.GetWeather(
                    parameters?.Location ?? string.Empty,
                    parameters?.Unit ?? string.Empty);

                responseMessage =
                    $"The current temperature in {weather.Location} is {weather.Temperature} {weather.Unit}.";
            }
        }
    }
    // Handle Regular Response
    else
    {
        responseMessage = response.Value.Choices[0].Message.Content;
    }

    // Add assistant response message to history context
    chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(responseMessage));

    Console.WriteLine($"Assistant: {responseMessage}");
}

Console.WriteLine("Conversation ended!");
Console.Read();