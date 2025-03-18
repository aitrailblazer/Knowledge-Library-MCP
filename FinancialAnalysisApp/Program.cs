using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Retrieve connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable("AIPROJECT_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: AIPROJECT_CONNECTION_STRING environment variable not set.");
            return;
        }

        // Specify your PDF file path
        string filePath = "TSLA-10-K_20250315_100652.pdf"; // Adjust this path if needed

        // Check if the file exists locally
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: The file '{filePath}' does not exist in the current directory.");
            return;
        }

        try
        {
            // Create AgentsClient
            var client = new AgentsClient(connectionString, new DefaultAzureCredential());

            // Extract company ticker from the file name
            string companyTicker = ExtractCompanyTicker(filePath);
            string vectorStoreName = companyTicker; // Vector store named after ticker (e.g., "TSLA")

            // Check if vector store exists or create/update it with the file
            VectorStore? vectorStore = await GetOrUpdateVectorStoreAsync(client, filePath, vectorStoreName);
            if (vectorStore == null)
            {
                Console.WriteLine("Failed to get or update vector store.");
                return;
            }

            // Create a unique agent name based on the ticker
            string agentName = $"Agent for {companyTicker}";
            Agent agent = await GetOrCreateAgentAsync(client, vectorStore, agentName, companyTicker);

            // Interactive loop for querying the agent
            Console.WriteLine("Enter a question about the file contents (e.g., 'From the 10-K filing, what was the revenue?' or press Esc to exit):");
            while (true)
            {
                Console.Write("> ");
                string userInput = ReadLineWithEsc();
                if (userInput == null) // Esc pressed
                {
                    Console.WriteLine("Exiting...");
                    break;
                }

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("Please enter a valid question.");
                    continue;
                }

                // Create a thread
                var threadResponse = await client.CreateThreadAsync();
                var thread = threadResponse.Value;
                Console.WriteLine($"Thread created with ID: {thread.Id}");

                // Create a message in the thread
                var messageResponse = await client.CreateMessageAsync(
                    thread.Id,
                    MessageRole.User,
                    userInput
                );
                var message = messageResponse.Value;
                Console.WriteLine("Message created.");

                // Create and execute a run
                var runResponse = await client.CreateRunAsync(
                    thread.Id,
                    agent.Id,
                    additionalInstructions: "Use the file data to answer the question."
                );
                var run = runResponse.Value;
                Console.WriteLine($"Run started with ID: {run.Id}");

                // Poll the run until it's complete
                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
                    run = runResponse.Value;
                    Console.Write($"Run status: {run.Status}...");
                }
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
                Console.WriteLine("\nRun completed.");

                // Retrieve and display messages
                var afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
                var messages = afterRunMessagesResponse.Value.Data;

                Console.WriteLine("Messages from the thread:");
                foreach (var threadMessage in messages.OrderByDescending(m => m.CreatedAt))
                {
                    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                    foreach (var contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            Console.WriteLine(textItem.Text);
                        }
                        else if (contentItem is MessageImageFileContent imageFileItem)
                        {
                            Console.WriteLine($"<image from ID: {imageFileItem.FileId}");
                        }
                    }
                }

                Console.WriteLine("\nEnter another question (or press Esc to exit):");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    // Check if vector store exists or create/update it with the file
    private static async Task<VectorStore?> GetOrUpdateVectorStoreAsync(AgentsClient client, string filePath, string vectorStoreName)
    {
        Console.WriteLine($"Checking if vector store '{vectorStoreName}' exists...");

        // Speculative: Check for existing vector stores
        VectorStore? existingVectorStore = null;
        try
        {
            var vectorStoresResponse = await client.GetVectorStoresAsync(); // Hypothetical method
            existingVectorStore = vectorStoresResponse.Value.Data.FirstOrDefault(v => v.Name == vectorStoreName);
            if (existingVectorStore != null)
            {
                Console.WriteLine($"Vector store '{vectorStoreName}' already exists with ID: {existingVectorStore.Id}.");
            }
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Vector store listing not supported; proceeding with creation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking vector stores: {ex.Message}. Proceeding with creation.");
        }

        // Upload the new file
        Console.WriteLine($"Uploading file '{filePath}'...");
        Response<AgentFile> uploadAgentFileResponse = await client.UploadFileAsync(
            filePath: filePath,
            purpose: AgentFilePurpose.Agents);
        if (uploadAgentFileResponse == null || uploadAgentFileResponse.Value == null)
        {
            Console.WriteLine($"Failed to upload file '{filePath}'.");
            return null;
        }
        AgentFile uploadedAgentFile = uploadAgentFileResponse.Value;
        Console.WriteLine($"File uploaded with ID: {uploadedAgentFile.Id}");

        // If vector store exists, update it with the new file ID
        if (existingVectorStore != null)
        {
            Console.WriteLine($"Updating vector store '{vectorStoreName}' to include new file...");
            // Hypothetical UpdateVectorStoreAsync method
            VectorStore vectorStore = await client.UpdateVectorStoreAsync(
                vectorStoreId: existingVectorStore.Id,
                fileIdsToAdd: new List<string> { uploadedAgentFile.Id });
            if (vectorStore == null)
            {
                Console.WriteLine("Failed to update vector store.");
                return null;
            }
            Console.WriteLine($"Vector store updated with ID: {vectorStore.Id}");
            return vectorStore;
        }
        else
        {
            // Create vector store with the uploaded file if it doesn't exist
            Console.WriteLine($"Creating vector store '{vectorStoreName}'...");
            VectorStore vectorStore = await client.CreateVectorStoreAsync(
                fileIds: new List<string> { uploadedAgentFile.Id },
                name: vectorStoreName);
            if (vectorStore == null)
            {
                Console.WriteLine("Failed to create vector store.");
                return null;
            }
            Console.WriteLine($"Vector store created with ID: {vectorStore.Id}");
            return vectorStore;
        }
    }

    // Check if agent exists or create it with a unique name
    private static async Task<Agent> GetOrCreateAgentAsync(AgentsClient client, VectorStore vectorStore, string agentName, string companyTicker)
    {
        Console.WriteLine($"Checking if agent '{agentName}' exists...");

        // Speculative: Check for existing agents
        try
        {
            var agentsResponse = await client.GetAgentsAsync(); // Hypothetical method
            var existingAgent = agentsResponse.Value.Data.FirstOrDefault(a => a.Name == agentName);
            if (existingAgent != null)
            {
                Console.WriteLine($"Agent '{agentName}' already exists with ID: {existingAgent.Id}");
                return existingAgent;
            }
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Agent listing not supported; proceeding with creation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking agents: {ex.Message}. Proceeding with creation.");
        }

        // Create agent if not found
        Console.WriteLine("Creating agent with file search...");
        FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
        fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

        Response<Agent> agentResponse = await client.CreateAgentAsync(
            model: "gpt-4o",
            name: agentName,
            instructions: $"You are a helpful agent for {companyTicker}. Your vector store contains all filings for this company, including annual reports (files with '10-K' in name) and quarterly updates (files with 'Q4-2024-Update' or similar). When asked to search in a specific filing, filter to files matching the name pattern (e.g., '10-K' for annual reports, 'Q4-2024-Update' for quarterly updates). If no specific filing is mentioned, search across all filings.",
            tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
            toolResources: new ToolResources() { FileSearch = fileSearchToolResource });
        Agent agent = agentResponse.Value;
        Console.WriteLine($"Agent created: {agent.Name}");
        return agent;
    }

    // Helper method to extract company ticker from file name
    private static string ExtractCompanyTicker(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName.Split('-')[0]; // Assumes format "Ticker-FilingDetails"
    }

    // Helper method to read input and detect Esc key
    private static string ReadLineWithEsc()
    {
        string input = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null; // Indicates Esc was pressed
            }
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input;
            }
            if (key.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                input += key.KeyChar;
                Console.Write(key.KeyChar);
            }
        }
    }
}