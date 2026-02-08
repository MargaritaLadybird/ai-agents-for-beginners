#!/usr/bin/dotnet run

#:package Microsoft.Extensions.AI@10.1.1
#:package Microsoft.Extensions.AI.OpenAI@10.1.1-preview.1.25612.2
#:package Microsoft.Agents.AI.OpenAI@1.0.0-preview.251219.1

using System.ClientModel;
using System.ComponentModel;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

// Tool Function: Flight Availability Checker
// This static method will be available to the agent as a callable tool
// The [Description] attribute helps the AI understand when to use this function
[Description("Checks if flights are available to a destination based on current political or war situation.")]
static string CheckFlightAvailability(string destination)

{
    // Tool Function: Random Destination Generator
    // List of affected countries (example, can be expanded)
    var affectedCountries = new List<string>
    {
        "Ukraine",
        "Russia",
        "Israel",
        "Syria",
        "Afghanistan",
        "Sudan",
        "Yemen",
        "Myanmar",
        "Iran",
        "North Korea"
    };

    // Check if destination contains any affected country
    foreach (var country in affectedCountries)
    {
        if (destination.IndexOf(country, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return $"Note: Flights to {destination} may be unavailable or disrupted due to current political or war situation.";
        }
    }
    // If not affected, return empty string (no note)
    return string.Empty;
}


// The [Description] attribute helps the AI understand when to use this function
// This demonstrates how to create custom tools for AI agents
[Description("Provides a random vacation destination.")]
static string GetRandomDestination()
{
    // List of popular vacation destinations around the world
    // The agent will randomly select from these options
    var destinations = new List<string>
    {
        "Paris, France",
        "Tokyo, Japan",
        "New York City, USA",
        "Sydney, Australia",
        "Rome, Italy",
        "Barcelona, Spain",
        "Cape Town, South Africa",
        "Rio de Janeiro, Brazil",
        "Bangkok, Thailand",
        "Vancouver, Canada"
    };

    // Generate random index and return selected destination
    // Uses System.Random for simple random selection
    var random = new Random();
    int index = random.Next(destinations.Count);
    return destinations[index];
}

// Extract configuration from environment variables
// Retrieve the GitHub Models API endpoint, defaults to https://models.github.ai/inference if not specified
// Retrieve the model ID, defaults to openai/gpt-5-mini if not specified
// Retrieve the GitHub token for authentication, throws exception if not specified
var github_endpoint = Environment.GetEnvironmentVariable("GH_ENDPOINT") ?? "https://models.github.ai/inference";
var github_model_id = Environment.GetEnvironmentVariable("GH_MODEL_ID") ?? "openai/gpt-5-mini";
var github_token = Environment.GetEnvironmentVariable("GH_TOKEN") ?? throw new InvalidOperationException("GH_TOKEN is not set.");

// Configure OpenAI Client Options
// Create configuration options to point to GitHub Models endpoint
// This redirects OpenAI client calls to GitHub's model inference service
var openAIOptions = new OpenAIClientOptions()
{
    Endpoint = new Uri(github_endpoint)
};

// Initialize OpenAI Client with GitHub Models Configuration
// Create OpenAI client using GitHub token for authentication
// Configure it to use GitHub Models endpoint instead of OpenAI directly
var openAIClient = new OpenAIClient(new ApiKeyCredential(github_token), openAIOptions);

// Create AI Agent with Travel Planning Capabilities
// Initialize complete agent pipeline: OpenAI client → Chat client → AI agent
// Configure agent with name, instructions, and available tools
// The agent can now plan trips using the GetRandomDestination function
AIAgent agent = openAIClient
    .GetChatClient(github_model_id)
    .AsIChatClient()
    .CreateAIAgent(
        instructions: "You are a helpful AI Agent that can help plan vacations for customers at random destinations",
        tools: [AIFunctionFactory.Create(GetRandomDestination), AIFunctionFactory.Create(CheckFlightAvailability) ]
    );

// Execute Agent: Plan a Day Trip
// Prompt user for a custom message
Console.Write("Enter your desired destination (leave blank for a random suggestion): ");
string userInput = Console.ReadLine() ?? "";

string agentPrompt;
if (string.IsNullOrWhiteSpace(userInput))
{
    agentPrompt = "I want to go on vacation. Please recommend a random destination and plan a day trip. Add a fun fact about the destination. Suggest a best airplane company to fly with and a restaurant to eat at. Also check if flights are available to the recommended destination due to current political or war situation.";
}
else
{
    agentPrompt = $"I want to go on vacation to {userInput}. Please plan a day trip for me at that destination. Add a fun fact about the destination. Suggest a best airplane company to fly with and a restaurant to eat at. Also check if flights are available to {userInput} due to current political or war situation.";
}

// Run the agent with streaming enabled for real-time response display
await foreach (var update in agent.RunStreamingAsync(agentPrompt))
{
    await Task.Delay(10);
    Console.Write(update);
}
