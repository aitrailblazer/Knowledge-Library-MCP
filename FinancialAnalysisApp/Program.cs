using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

    private const float Temperature = 0.5f;
    private const float TopP = 0.9f;

    private static Dictionary<string, (string ParentTool, Dictionary<string, object> Subtool)> mcpTools = new();

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

        await FetchMcpToolsAsync();

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
            Console.WriteLine("    Example: dotnet run TSLA--10-K--20250315_100652.pdf");
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
            Agent agent = await GetOrCreateAgentAsync(client, vectorStore, agentName, companyTicker, formName, vectorStoreName, date);

            await ChatLoop(client, agent, filePath, vectorStoreName, companyTicker, formName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static async Task FetchMcpToolsAsync()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080/") };
        try
        {
            var toolsResponse = await httpClient.GetStringAsync("/tools");
            var toolsData = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(toolsResponse);
            if (toolsData != null && toolsData.TryGetValue("tools", out var tools))
            {
                Console.WriteLine("MCP Server Tools Available:");
                foreach (var tool in tools)
                {
                    var parentToolName = tool["name"]?.ToString();
                    if (string.IsNullOrEmpty(parentToolName)) continue;
                    Console.WriteLine($"- {parentToolName}");
                    var subtools = tool["subtools"] as List<object>;
                    if (subtools != null)
                    {
                        foreach (var subtool in subtools)
                        {
                            var st = subtool as Dictionary<string, object>;
                            if (st != null && st.TryGetValue("name", out var nameObj))
                            {
                                var subtoolName = nameObj?.ToString();
                                if (string.IsNullOrEmpty(subtoolName)) continue;
                                var mappedName = subtoolName switch
                                {
                                    "BraveSearch" => "brave_web_search",
                                    "YahooStockPrice" => "yahoo_stock_price",
                                    _ => subtoolName.ToLower().Replace(" ", "_")
                                };
                                mcpTools[mappedName] = (parentToolName, st);
                                Console.WriteLine($"  - {mappedName}: {st["description"]}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not fetch MCP tools from /tools: {ex.Message}. Proceeding without MCP tools.");
        }
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
                        string? confirmation = Console.ReadLine()?.Trim().ToLower();
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

    private static async Task ChatLoop(AgentsClient client, Agent agent, string filePath, string vectorStoreName, string companyTicker, string formName)
    {
        Console.WriteLine($"Chatting with file '{filePath}' using agent '{agent.Name}' (Vector Store: '{vectorStoreName}'). Enter a question (or press Esc to exit):");
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080/") };

        while (true)
        {
            Console.Write("> ");
            string? userInput = ReadLineWithEsc();
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

                // Explicitly force the use of yahoo_stock_price for stock price queries
                string additionalInstructions = $"You are a financial analysis agent. For any query asking for the current stock price (e.g., 'TSLA stock price today'), you MUST use the 'yahoo_stock_price' tool at http://localhost:8080/invoke with the ticker '{companyTicker}' unless another ticker is specified. For historical data, prioritize the vector store '{vectorStoreName}' from the {companyTicker} {formName} filing dated {Path.GetFileNameWithoutExtension(filePath).Split('_')[0].Split("--")[2]}. Discover tools at http://localhost:8080/tools. Answer in Markdown format.";

                var runResponse = await client.CreateRunAsync(
                    threadId: thread.Id,
                    assistantId: agent.Id,
                    additionalInstructions: additionalInstructions,
                    temperature: Temperature,
                    topP: TopP
                );
                if (runResponse?.Value == null)
                    throw new InvalidOperationException("Failed to start run.");
                var run = runResponse.Value;
                Console.WriteLine($"Run started with ID: {run.Id}");

                do
                {
                    await Task.Delay(500);
                    runResponse = await client.GetRunAsync(thread.Id, run.Id);
                    if (runResponse?.Value == null)
                        throw new InvalidOperationException("Failed to get run status.");
                    run = runResponse.Value;
                    Console.Write($"Run status: {run.Status}...");

                    if (run.Status == RunStatus.RequiresAction && run.RequiredActions != null)
                    {
                        Console.WriteLine("\nTool action required.");
                        foreach (var action in run.RequiredActions)
                        {
                            var toolCall = action as RequiredFunctionToolCall;
                            if (toolCall == null)
                            {
                                Console.WriteLine("Warning: Action is not a function tool call. Skipping.");
                                continue;
                            }

                            Console.WriteLine($"Tool call detected: {toolCall.Name}");
                            if (!mcpTools.ContainsKey(toolCall.Name))
                            {
                                Console.WriteLine($"Error: Tool '{toolCall.Name}' not found in MCP capabilities.");
                                continue;
                            }

                            var (parentTool, subtool) = mcpTools[toolCall.Name];
                            var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(toolCall.Arguments) ?? new Dictionary<string, string>();
                            Console.WriteLine($"Tool arguments: {toolCall.Arguments}");

                            var mcpPayload = new
                            {
                                tool = parentTool,
                                parameters = new Dictionary<string, object> { { "operation", subtool["name"] } }
                            };

                            foreach (var param in parameters)
                            {
                                mcpPayload.parameters[param.Key] = param.Value;
                            }

                            if (toolCall.Name == "yahoo_stock_price")
                            {
                                if (!parameters.ContainsKey("ticker")) mcpPayload.parameters["ticker"] = companyTicker;
                                if (!parameters.ContainsKey("interval")) mcpPayload.parameters["interval"] = "1d";
                                if (!parameters.ContainsKey("period")) mcpPayload.parameters["period"] = "1d"; // Use 1d for current price
                            }

                            var payloadJson = JsonSerializer.Serialize(mcpPayload);
                            Console.WriteLine($"Sending payload to MCP: {payloadJson}");
                            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                            var mcpResponse = await httpClient.PostAsync("invoke", content);
                            var mcpResult = await mcpResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"MCP response (Status: {mcpResponse.StatusCode}): {mcpResult}");

                            string? toolResult = null;
                            if (mcpResponse.IsSuccessStatusCode)
                            {
                                var resultDict = JsonSerializer.Deserialize<Dictionary<string, string>>(mcpResult);
                                toolResult = resultDict?.ContainsKey("result") == true ? resultDict["result"] : mcpResult;
                            }
                            else
                            {
                                Console.WriteLine($"Error: MCP request failed with status {mcpResponse.StatusCode}");
                            }

                            if (toolResult != null)
                            {
                                await client.SubmitToolOutputsToRunAsync(run, new List<ToolOutput> { new ToolOutput(toolCall.Id, toolResult) });
                                Console.WriteLine($"Submitted {toolCall.Name} result to agent: {toolResult}");
                            }
                            else
                            {
                                Console.WriteLine($"Error: No valid result from '{toolCall.Name}'. Response: {mcpResult}");
                                await client.SubmitToolOutputsToRunAsync(run, new List<ToolOutput> { new ToolOutput(toolCall.Id, "Error: Unable to fetch data") });
                            }
                        }
                    }
                } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction);

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
                            Console.WriteLine($"<image from ID: {imageFileItem.FileId}>");
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
        if (ImageExtensions.Contains(Path.GetExtension(filePath).ToLower()) || Path.GetExtension(filePath).ToLower() == ".pdf")
        {
            string markdownContent = await ExtractContentAsMarkdown(filePath);
            string directory = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
            uploadFilePath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(filePath)}_processed.md");
            File.WriteAllText(uploadFilePath, markdownContent);
            Console.WriteLine($"Markdown content extracted from file and saved to '{uploadFilePath}'. Verifying content...");
            Console.WriteLine($"Markdown content preview: {(markdownContent.Length > 100 ? markdownContent.Substring(0, 100) + "..." : markdownContent)}");
        }

        Console.WriteLine($"Uploading file '{uploadFilePath}'...");
        Azure.Response<AgentFile> uploadAgentFileResponse = await client.UploadFileAsync(filePath: uploadFilePath, purpose: AgentFilePurpose.Agents);
        if (uploadAgentFileResponse?.Value == null)
            throw new InvalidOperationException($"Failed to upload file '{uploadFilePath}'.");

        AgentFile uploadedAgentFile = uploadAgentFileResponse.Value;
        Console.WriteLine($"File uploaded successfully with ID: {uploadedAgentFile.Id}");

        Console.WriteLine($"Creating vector store '{vectorStoreName}' with file ID: {uploadedAgentFile.Id}...");
        Azure.Response<VectorStore> vectorStoreResponseCreate = await client.CreateVectorStoreAsync(fileIds: new List<string> { uploadedAgentFile.Id }, name: vectorStoreName);
        if (vectorStoreResponseCreate?.Value == null)
            throw new InvalidOperationException("Failed to create vector store.");

        VectorStore vectorStore = vectorStoreResponseCreate.Value;
        Console.WriteLine($"Vector store created with ID: {vectorStore.Id}");
        return vectorStore;
    }

    private static async Task<string> ExtractContentAsMarkdown(string filePath)
    {
        var endpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_API_KEY");
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Document Intelligence endpoint or API key not set in environment variables.");
        }

        var credential = new Azure.AzureKeyCredential(apiKey);
        var client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        Console.WriteLine($"Analyzing file '{filePath}' with Document Intelligence...");
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var content = BinaryData.FromStream(stream);
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
        var result = operation.Value;

        if (result == null || result.Pages == null || !result.Pages.Any())
        {
            Console.WriteLine("Warning: No content extracted from the file.");
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

    private static async Task<Agent> GetOrCreateAgentAsync(AgentsClient client, VectorStore vectorStore, string agentName, string companyTicker, string formName, string vectorStoreName, string date)
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

        var toolDefinitions = new List<string>();
        foreach (var (toolName, (_, subtool)) in mcpTools)
        {
            var parameters = subtool["parameters"] as Dictionary<string, object>;
            var properties = new Dictionary<string, object>();
            var required = new List<string>();
            foreach (var param in parameters)
            {
                var paramDetails = param.Value as Dictionary<string, object>;
                properties[param.Key] = new { type = paramDetails["type"], description = param.Key };
                if ((bool)paramDetails["required"])
                {
                    required.Add(param.Key);
                }
            }
            var toolJson = $@"{{
                ""type"": ""function"",
                ""function"": {{
                    ""name"": ""{toolName}"",
                    ""description"": ""{subtool["description"]}"",
                    ""parameters"": {{
                        ""type"": ""object"",
                        ""properties"": {JsonSerializer.Serialize(properties)},
                        ""required"": {JsonSerializer.Serialize(required)}
                    }}
                }}
            }}";
            toolDefinitions.Add(toolJson);
        }

        string instructions = formName.ToUpper() switch
        {
            "10-K" => $"You are a financial analysis agent for {companyTicker} specializing in annual 10-K filings. Your primary knowledge is the vector store '{vectorStoreName}' (dated {date}). Use this for historical financial details like performance, risks, and operations. For real-time data (e.g., current stock prices), you MUST use the 'yahoo_stock_price' tool at http://localhost:8080/invoke:\n{string.Join("\n", toolDefinitions)}\nAnswer in Markdown.",
            "Q4" => $"You are a financial analysis agent for {companyTicker} specializing in Q4 filings. Your primary knowledge is the vector store '{vectorStoreName}' (dated {date}). Use this for Q4 performance, earnings, and updates. For real-time data (e.g., current stock prices), you MUST use the 'yahoo_stock_price' tool at http://localhost:8080/invoke:\n{string.Join("\n", toolDefinitions)}\nAnswer in Markdown.",
            _ => $"You are a financial analysis agent for {companyTicker}. Your primary knowledge is the vector store '{vectorStoreName}' (dated {date}). Use this for filing-specific details. For real-time data (e.g., current stock prices), you MUST use the 'yahoo_stock_price' tool at http://localhost:8080/invoke:\n{string.Join("\n", toolDefinitions)}\nAnswer in Markdown."
        };

        Console.WriteLine($"Creating agent '{agentName}' for ticker '{companyTicker}' with vector store '{vectorStoreName}'...");
        Azure.Response<Agent> agentResponse = await client.CreateAgentAsync(
            model: "gpt-4o",
            name: agentName,
            instructions: instructions,
            tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
            toolResources: new ToolResources { FileSearch = fileSearchToolResource }
        );

        Agent agent = agentResponse?.Value ?? throw new InvalidOperationException("Failed to create agent.");
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

    private static string? ReadLineWithEsc()
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