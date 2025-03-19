using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.Projects;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".c", ".cpp", ".cs", ".css", ".doc", ".docx", ".go", ".html", ".java", ".js", 
        ".json", ".md", ".pdf", ".php", ".pptx", ".py", ".rb", ".sh", ".tex", ".ts", ".txt",
        ".png", ".jpg", ".tiff", ".bmp"
    };

    private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".tiff", ".bmp"
    };

    static async Task Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIPROJECT_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: AIPROJECT_CONNECTION_STRING environment variable not set.");
            return;
        }

        var userPrefix = Environment.GetEnvironmentVariable("USER_PREFIX") ?? "DefaultUser";
        Console.WriteLine($"Using user prefix: {userPrefix}");

        if (args.Length == 1 && (args[0] == "-l" || args[0] == "--list"))
        {
            try
            {
                var client = new AgentsClient(connectionString, new DefaultAzureCredential());
                await ListAndManageVectorStoresAsync(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while managing vector stores: " + ex.Message);
            }
            return;
        }

        if (args.Length != 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  - To chat with a file: dotnet run <filename>");
            Console.WriteLine("    Example: dotnet run TSLA--10-K--20250315_100652.png");
            Console.WriteLine("  - To list and manage vector stores: dotnet run -l or --list");
            Console.WriteLine("  Set USER_PREFIX environment variable for multi-user support (e.g., 'UserA')");
            return;
        }

        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: The file '{filePath}' does not exist in the current directory.");
            return;
        }

        string extension = Path.GetExtension(filePath).ToLower();
        if (!SupportedExtensions.Contains(extension))
        {
            Console.WriteLine($"Error: File extension '{extension}' is not supported. Supported types: {string.Join(", ", SupportedExtensions)}.");
            return;
        }

        var parsedFileName = ParseFileName(filePath);
        if (parsedFileName.companyTicker == null || parsedFileName.formName == null || parsedFileName.date == null)
        {
            Console.WriteLine("Error: Invalid filename format. Expected format: <ticker>--<form>--<date>_<timestamp>.<extension>");
            return;
        }
        string companyTicker = parsedFileName.companyTicker;
        string formName = parsedFileName.formName;
        string date = parsedFileName.date;

        try
        {
            var client = new AgentsClient(connectionString, new DefaultAzureCredential());
            string agentName = $"{userPrefix}_{companyTicker}-{formName}";
            string vectorStoreName = $"{formName}--{date}";

            VectorStore vectorStore = await GetOrCreateVectorStoreAsync(client, filePath, vectorStoreName);
            Agent agent = await GetOrCreateAgentAsync(client, vectorStore, agentName, companyTicker, formName, vectorStoreName);

            await ChatLoop(client, agent, filePath, vectorStoreName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task ListAndManageVectorStoresAsync(AgentsClient client)
    {
        try
        {
            var vectorStoresResponse = await client.GetVectorStoresAsync();
            if (vectorStoresResponse?.Value == null || !vectorStoresResponse.Value.Data.Any())
            {
                Console.WriteLine("No vector stores found in Azure.");
                return;
            }

            var vectorStores = vectorStoresResponse.Value.Data.ToList();
            int selectedIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Available Vector Stores in Azure:");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine($"{"Index",-6} | {"Name",-30} | {"ID",-30} | {"Created",-19}");
                Console.WriteLine("----------------------------------------");

                for (int i = 0; i < vectorStores.Count; i++)
                {
                    var vs = vectorStores[i];
                    string createdAt = vs.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"{i + 1,-6} | {vs.Name,-30} | {vs.Id,-30} | {createdAt,-19}";
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine(line);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine(line);
                    }
                }

                Console.WriteLine("\nUse ↑/↓ to navigate, Enter to delete, Esc to exit");

                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(vectorStores.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.Enter:
                        var selectedVectorStore = vectorStores[selectedIndex];
                        Console.WriteLine($"\nAre you sure you want to delete '{selectedVectorStore.Name}' (ID: {selectedVectorStore.Id})? (y/n)");
                        Console.Write("> ");
                        string confirmation = Console.ReadLine()?.Trim().ToLower() ?? "";
                        if (confirmation == "y")
                        {
                            try
                            {
                                await client.DeleteVectorStoreAsync(selectedVectorStore.Id);
                                Console.WriteLine($"Vector store '{selectedVectorStore.Name}' (ID: {selectedVectorStore.Id}) deleted successfully.");
                                vectorStores.RemoveAt(selectedIndex);
                                if (vectorStores.Count == 0)
                                {
                                    Console.WriteLine("No vector stores remaining. Press any key to exit.");
                                    Console.ReadKey(true);
                                    return;
                                }
                                if (selectedIndex >= vectorStores.Count)
                                    selectedIndex = vectorStores.Count - 1;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error deleting vector store: {ex.Message}");
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey(true);
                            }
                        }
                        break;
                    case ConsoleKey.Escape:
                        Console.WriteLine("Exiting vector store management...");
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error listing vector stores: " + ex.Message);
        }
    }

    private static async Task ChatLoop(AgentsClient client, Agent agent, string filePath, string vectorStoreName)
    {
        Console.WriteLine($"Chatting with file '{filePath}' using agent '{agent.Name}' (Vector Store: '{vectorStoreName}'). Enter a question (or press Esc to exit):");
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

            try
            {
                var threadResponse = await client.CreateThreadAsync();
                if (threadResponse?.Value == null)
                    throw new InvalidOperationException("Failed to create thread.");
                var thread = threadResponse.Value;
                Console.WriteLine($"Thread created with ID: {thread.Id}");

                var messageResponse = await client.CreateMessageAsync(thread.Id, MessageRole.User, userInput);
                if (messageResponse?.Value == null)
                    throw new InvalidOperationException("Failed to create message.");
                Console.WriteLine("Message sent.");

                var runResponse = await client.CreateRunAsync(
                    thread.Id,
                    agent.Id,
                    additionalInstructions: $"Use the file data from the vector store '{vectorStoreName}' to answer the question in Markdown format."
                );
                if (runResponse?.Value == null)
                    throw new InvalidOperationException("Failed to start run.");
                var run = runResponse.Value;
                Console.WriteLine($"Run started with ID: {run.Id}");

                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    runResponse = await client.GetRunAsync(thread.Id, run.Id);
                    if (runResponse?.Value == null)
                        throw new InvalidOperationException("Failed to get run status.");
                    run = runResponse.Value;
                    Console.Write($"Run status: {run.Status}...");
                }
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

                if (run.Status != RunStatus.Completed)
                {
                    Console.WriteLine($"\nRun failed with status: {run.Status}");
                    continue;
                }
                Console.WriteLine("\nRun completed.");

                var messagesResponse = await client.GetMessagesAsync(thread.Id);
                if (messagesResponse?.Value == null)
                    throw new InvalidOperationException("Failed to retrieve messages.");
                var messages = messagesResponse.Value.Data;

                var assistantMessage = messages
                    .Where(m => m.Role.ToString().Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                if (assistantMessage != null)
                {
                    Console.WriteLine("Assistant's response:");
                    foreach (var contentItem in assistantMessage.ContentItems)
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
                else
                {
                    Console.WriteLine("No response from the assistant.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in chat loop: {ex.Message}");
            }

            Console.WriteLine("\nEnter another question (or press Esc to exit):");
        }
    }

#pragma warning disable CS8603 // Suppress false positive CS8603 warning
    private static async Task<VectorStore> GetOrCreateVectorStoreAsync(AgentsClient client, string filePath, string vectorStoreName)
    {
        Console.WriteLine($"Checking if vector store '{vectorStoreName}' exists in Azure...");

        VectorStore? existingVectorStore = null;
        try
        {
            var vectorStoresResponse = await client.GetVectorStoresAsync();
            if (vectorStoresResponse?.Value == null)
                throw new InvalidOperationException("Failed to retrieve vector stores.");
            existingVectorStore = vectorStoresResponse.Value.Data.FirstOrDefault(v => v.Name == vectorStoreName);
            if (existingVectorStore != null)
            {
                Console.WriteLine($"Vector store '{vectorStoreName}' already exists with ID: {existingVectorStore.Id}. Using it as is.");
                return existingVectorStore;
            }
            Console.WriteLine($"Vector store '{vectorStoreName}' does not exist. Proceeding to create it.");
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine("Vector store listing not supported; proceeding with creation: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking vector stores; proceeding with creation: {ex.Message}");
        }

        string uploadFilePath = filePath;
        if (ImageExtensions.Contains(Path.GetExtension(filePath).ToLower()))
        {
            // Preprocess image files with Document Intelligence
            string markdownContent = await ExtractContentAsMarkdown(filePath);
            string directory = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
            uploadFilePath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(filePath)}_processed.md");
            File.WriteAllText(uploadFilePath, markdownContent);
            Console.WriteLine($"Markdown content extracted from image and saved to '{uploadFilePath}'. Verifying content...");
            Console.WriteLine($"Markdown content preview: {(markdownContent.Length > 100 ? markdownContent.Substring(0, 100) + "..." : markdownContent)}");
        }

        // Upload the file
        Console.WriteLine($"Uploading file '{uploadFilePath}'...");
        Response<AgentFile> uploadAgentFileResponse = await client.UploadFileAsync(filePath: uploadFilePath, purpose: AgentFilePurpose.Agents);
        if (uploadAgentFileResponse?.Value == null)
            throw new InvalidOperationException($"Failed to upload file '{uploadFilePath}'.");

        AgentFile uploadedAgentFile = uploadAgentFileResponse.Value;
        Console.WriteLine($"File uploaded successfully with ID: {uploadedAgentFile.Id}");

        // Create the vector store
        Console.WriteLine($"Creating vector store '{vectorStoreName}' with file ID: {uploadedAgentFile.Id}...");
        Response<VectorStore> vectorStoreResponseCreate = await client.CreateVectorStoreAsync(fileIds: new List<string> { uploadedAgentFile.Id }, name: vectorStoreName);
        if (vectorStoreResponseCreate?.Value == null)
            throw new InvalidOperationException("Failed to create vector store.");

        VectorStore vectorStore = vectorStoreResponseCreate.Value;
        Console.WriteLine($"Vector store created with ID: {vectorStore.Id}");
        return vectorStore;
    }
#pragma warning restore CS8603

    private static async Task<string> ExtractContentAsMarkdown(string filePath)
    {
        var endpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY");
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Document Intelligence endpoint or API key not set in environment variables.");
        }

        var credential = new AzureKeyCredential(apiKey);
        var client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        Console.WriteLine($"Analyzing image '{filePath}' with Document Intelligence...");
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var content = BinaryData.FromStream(stream);
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
        var result = operation.Value;

        if (result == null || result.Pages == null || !result.Pages.Any())
        {
            Console.WriteLine("Warning: No content extracted from the image.");
            return string.Empty;
        }

        var markdown = new StringBuilder();

        foreach (var page in result.Pages)
        {
            foreach (var line in page.Lines)
            {
                markdown.AppendLine(line.Content);
            }

            foreach (var table in result.Tables)
            {
                if (table.RowCount > 0 && table.ColumnCount > 0)
                {
                    var headers = new List<string>();
                    for (int col = 0; col < table.ColumnCount; col++)
                    {
                        var cell = table.Cells.FirstOrDefault(c => c.RowIndex == 0 && c.ColumnIndex == col);
                        headers.Add(cell?.Content ?? "");
                    }
                    markdown.AppendLine("| " + string.Join(" | ", headers) + " |");
                    markdown.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", table.ColumnCount)) + " |");

                    for (int row = 1; row < table.RowCount; row++)
                    {
                        var rowCells = new List<string>();
                        for (int col = 0; col < table.ColumnCount; col++)
                        {
                            var cell = table.Cells.FirstOrDefault(c => c.RowIndex == row && c.ColumnIndex == col);
                            rowCells.Add(cell?.Content ?? "");
                        }
                        markdown.AppendLine("| " + string.Join(" | ", rowCells) + " |");
                    }
                    markdown.AppendLine();
                }
            }
        }

        return markdown.ToString();
    }

    private static async Task<Agent> GetOrCreateAgentAsync(AgentsClient client, VectorStore vectorStore, string agentName, string companyTicker, string formName, string vectorStoreName)
    {
        Console.WriteLine($"Checking if agent '{agentName}' exists...");

        FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
        fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

        try
        {
            var agentsResponse = await client.GetAgentsAsync();
            var existingAgent = agentsResponse.Value.Data.FirstOrDefault(a => a.Name == agentName);
            if (existingAgent != null)
            {
                Console.WriteLine($"Agent '{agentName}' already exists with ID: {existingAgent.Id}. Deleting and recreating to attach new vector store '{vectorStoreName}'...");
                await client.DeleteAgentAsync(existingAgent.Id);
            }
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Agent listing not supported; proceeding with creation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking or deleting agent: {ex.Message}. Proceeding with creation.");
        }

        string instructions;
        switch (formName.ToUpper())
        {
            case "10-K":
                instructions = $"You are a helpful agent for {companyTicker} specializing in annual 10-K filings. Your knowledge is based on the vector store '{vectorStoreName}' attached to this query. Use the vector store data to provide detailed answers about the company's annual financial performance, risks, and operations.";
                break;
            case "Q4":
                instructions = $"You are a helpful agent for {companyTicker} specializing in quarterly Q4 filings. Your knowledge is based on the vector store '{vectorStoreName}' attached to this query. Use the vector store data to provide insights into the company's fourth-quarter performance, earnings, and updates.";
                break;
            default:
                instructions = $"You are a helpful agent for {companyTicker}. Your knowledge is based on the vector store '{vectorStoreName}' attached to this query. Use the vector store data to answer questions accurately about this specific filing.";
                break;
        }

        Console.WriteLine($"Creating agent '{agentName}' for ticker '{companyTicker}' with vector store '{vectorStoreName}'...");
        Response<Agent> agentResponse = await client.CreateAgentAsync(
            model: "gpt-4o",
            name: agentName,
            instructions: instructions,
            tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
            toolResources: new ToolResources() { FileSearch = fileSearchToolResource });
        
        if (agentResponse?.Value == null)
            throw new InvalidOperationException("Failed to create agent.");
        
        Agent agent = agentResponse.Value;
        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id}) with vector store ID: {vectorStore.Id}");
        return agent;
    }

    private static (string? companyTicker, string? formName, string? date) ParseFileName(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string[] parts = fileName.Split("--", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return (null, null, null);
        }

        string companyTicker = parts[0];
        string formName = parts[1];
        string datePart = parts[2];
        string date = datePart.Split('_')[0];

        return (companyTicker, formName, date);
    }

    private static string ReadLineWithEsc()
    {
        string input = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
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