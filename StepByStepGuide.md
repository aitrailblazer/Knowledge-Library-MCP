# Step-by-Step Guide to Create Your Azure AI Foundry Starter Project on macOS

This guide walks you through setting up an Azure account, configuring your macOS environment, and implementing your financial analysis app using the Azure AI Agent Service SDK with the Responses API. The app processes a PDF (e.g., `TSLA-10-K_20250315_100652.pdf`) for text and image analysis, meeting the competition's multi-modal requirements.

---

## Step 1: Set Up Your Azure Account from the Command Line

### 1.1 Install Azure CLI
- Open **Terminal** on macOS.
- Install Azure CLI using Homebrew:
```bash
  brew install azure-cli
```

Verify installation:

```bash
az --version
```

You should see the CLI version (e.g., 2.58.0 or later).

### Step 1.2 Log In to Azure

Log in to your Azure account:

```bash
az login
```

A browser window will open; sign in with your Microsoft account. If you don’t have an account, create one at Azure Free Account.

After login, the terminal displays your subscription details:

```json
[
  {
    "id": "<subscription-id>",
    "name": "Free Trial",
    "state": "Enabled",
    ...
  }
]
```

### 1.3 Set Your Subscription

If you have multiple subscriptions, set the active one:

```bash
az account set --subscription "<subscription-id>"
```

Replace <subscription-id> with the ID from the login output.

### 1.4 Create a Resource Group

Create a resource group for your project:

```bash
az group create --name ait-ai-resources --location eastus2
```

This matches your connection string’s region (eastus2).

### 1.5 Create an Azure AI Foundry Hub (Manual Step)

Azure CLI doesn’t fully support creating AI Foundry hubs yet, so use the Azure Portal:
Go to portal.azure.com.

Search for “Azure AI Foundry”, click “Create AI Hub”.

Configure:
Subscription: Your active subscription.

Resource Group: ait-ai-resources.

Region: East US 2.

Name: FinancialHub.

Deploy and wait for completion (5-10 minutes).

### 1.6 Create an AI Project with GPT-4o Model
In the portal, navigate to your hub (FinancialHub), then create a project:
Name: ait-ai-project.

Add GPT-4o Model:
During project creation, under “Model Selection” or “Deployments”, select “GPT-4o” from the available models.

If not visible, deploy it manually:
Go to “Model Catalog” in the hub or Azure AI Studio.

Search for “GPT-4o”, click “Deploy”, and configure:
Deployment Name: gpt-4o.

Instance Type: Standard (adjust based on needs).

Confirm deployment in the project’s “Deployments” section.

Retrieve the connection string:
In ait-ai-project settings, find “Connection Strings” or “Keys and Endpoint”.

Expected: Endpoint=https://eastus2.api.azureml.ms;Key=<your-key>.

Your string (eastus2.api.azureml.ms;1234;ait-ai-resources;ait-ai-project) is incomplete; use the portal version if available.


### 1.7 Assign Permissions
Assign the “Azure AI Developer” role via CLI:

```bash
az role assignment create --role "Azure AI Developer" --assignee "<your-email>" --scope "/subscriptions/<subscription-id>/resourceGroups/ait-ai-resources"
```

## Step 2: Set Up Your macOS Development Environment

### 2.1 Install .NET SDK
Install .NET 9.0 SDK:

```bash
brew install dotnet-sdk
```

Verify:

```bash
dotnet --version
```

Output: 9.0.xxx.

### 2.2 Create a Project Directory

reate and navigate to your project folder:

```bash
mkdir FinancialAnalysisApp
cd FinancialAnalysisApp
```

### 2.3 Initialize a New Console Project
Create a new console app:

```bash
dotnet new console -n FinancialAnalysisApp
cd FinancialAnalysisApp
```

### 2.4 Add Required NuGet Packages

Install packages as per the SDK overview:

```bash
dotnet add package Azure.AI.Projects --prerelease
dotnet add package Azure.Core
dotnet add package Azure.Identity
```

Note: --prerelease is used since the SDK might be in preview.


## Step 3: Configure Environment Variables

### Step 3.1: Set the Connection String

Your code uses an environment variable AIPROJECT_CONNECTION_STRING. Set it in Terminal:

```bash
export AIPROJECT_CONNECTION_STRING="eastus2.api.azureml.ms;1234;ait-ai-resources;ait-ai-project"
```

Persist it for your session or add to ~/.zshrc (for Zsh, default on macOS):

```bash
echo 'export AIPROJECT_CONNECTION_STRING="eastus2.api.azureml.ms;1234;ait-ai-resources;ait-ai-project"' >> ~/.zshrc
source ~/.zshrc
```

## Step 4: Implement the Code

### 4.1 Replace Program.cs

Open Program.cs in a text editor (e.g., VS Code):

```bash
code Program.cs
```

```csharp
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

    private static async Task<VectorStore?> GetOrUpdateVectorStoreAsync(AgentsClient client, string filePath, string vectorStoreName)
    {
        Console.WriteLine($"Checking if vector store '{vectorStoreName}' exists...");

        VectorStore? existingVectorStore = null;
        try
        {
            var vectorStoresResponse = await client.GetVectorStoresAsync();
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

        if (existingVectorStore != null)
        {
            Console.WriteLine($"Updating vector store '{vectorStoreName}' to include new file...");
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

    private static async Task<Agent> GetOrCreateAgentAsync(AgentsClient client, VectorStore vectorStore, string agentName, string companyTicker)
    {
        Console.WriteLine($"Checking if agent '{agentName}' exists...");

        try
        {
            var agentsResponse = await client.GetAgentsAsync();
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

    private static string ExtractCompanyTicker(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName.Split('-')[0];
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
```

### 4.2 Prepare the PDF

Place TSLA-10-K_20250315_100652.pdf in the project directory:

```bash
cp /path/to/TSLA-10-K_20250315_100652.pdf .
```

## Step 5: Build and Run the Project

### 5.1: Compile the code:

```bash
dotnet build
```

### 5.2 Run the Project

```bash
dotnet run
```

### 5.3 Test Queries
Enter questions:
From the 10-K filing, what was the revenue?

What does the chart show about deliveries?

Press Esc to exit.

