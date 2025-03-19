# Project Story

## About the Project

**Knowledge Library MCP (KL MCP)** is a multi-modal app I developed to tackle the chaos of managing thousands of documents—Machine Learning files, SEC filings like TSLA 10-Ks and 10-Qs, process workflows, and charts—by finding them fast and enabling chat-based querying with Azure AI Agent Service. Below, I’ll share what inspired me, what I learned, how I built it, and the challenges I faced.

![Architecture Diagram](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-1-front.png)  
*The library-like architecture of KL MCP, powered by Azure AI Agent Service and MCP bots.*

## Inspiration

My inspiration came from wrestling with documentation overload as a developer—hours lost digging through PDFs, Excel sheets, and charts just to find the right info. Anthropic’s Model Context Protocol (MCP) caught my eye, showing how context could unlock insights from scattered data. I initially tried chunking documents and vectorizing them in Cosmos DB NoSQL, but as the pile grew, it became impossible to pinpoint specifics. I envisioned a smarter system, like a library, to organize and chat with documents using Azure’s AI tools—multi-modal, scalable, and responsible—turning frustration into a hackathon challenge worth solving.

![Initial Chaos](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06.png)  
*My early attempts at managing documentation chaos inspired a library-like solution.*

## What it does

KL MCP finds documents across text and images—like ML pipelines, financial filings, and workflows—then lets me chat with them to extract insights. It processes PDFs, Word, Excel, PowerPoint, text files, HTML, and images (via OCR/labels), using Azure AI Agent Service’s language smarts to answer queries like “What’s the revenue outlook?” or “Optimize this process?” My bot-driven system outscales Azure AI Search, blending live data and code-driven insights for a seamless, chat-ready experience.

![KL MCP Demo](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-front.png)  
*KL MCP finds documents and enables chat-based querying across text and images.*

## How we built it

I built KL MCP solo, leveraging Azure AI Agent Service’s ecosystem and using GitHub Copilot extensively to accelerate coding and refine logic:

- **Knowledge Tools with MCP Bots:** I coded a hierarchical system—DocBot for ML/workflows, SECBot for filings—where one agent oversees domains like TSLA or ML docs. With Copilot’s help, my scripts upload files into vector stores, while Cosmos DB NoSQL stores metadata (e.g., type, date), vectorized for semantic search. Text and images are processed with precision, dynamically attached for chat—unlike Search’s static indexes.
- **Azure Functions:** Copilot assisted in scripting API calls (e.g., stock prices) for live data, fixing RAG’s stale-data issue.
- **Code Interpreter:** I used Copilot to write Python code that crunches data or plots visuals—like profit trends—enhancing chat responses.
- **Azure AI Studio Agent:** With Copilot’s suggestions, I set up a GPT-4o agent, coded it to “Find files, call functions, run code, and chat,” integrating multi-modal insights with Responsible AI.
- **C# Client:** Copilot streamlined my coding of a lean app to manage it all—uploading, threading, querying—via the Azure AI Agents SDK.

## Challenges we ran into

Scaling was the biggest hurdle—my initial Cosmos DB NoSQL chunking couldn’t handle thousands of files without losing precision. Mapping a library-like hierarchy to Azure’s tools took trial and error; syncing metadata in Cosmos DB NoSQL with vector stores was complex. Integrating live data and code outputs while upholding Responsible AI standards—fairness, transparency—pushed my skills, but Copilot and debug cycles got me through.

## Accomplishments that we're proud of

I’m proud of crafting a multi-modal app that seamlessly handles text and images—finding a TSLA 10-K’s chart and chatting about its trends in seconds. Outscaling Azure AI Search with my bot-driven system is a big win, as is blending live data and code insights—all while embedding Responsible AI. Seeing it solve real developer pain points is a thrill.

![Chat Result](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-02.png)  
*A screenshot of KL MCP chatting about a TSLA 10-K chart, blending text and image insights.*

## What we learned

I learned Azure AI Agent Service’s strength for multi-modal apps—text and image processing with language smarts is transformative. Hierarchical systems beat flat indexes for scale, and Responsible AI is a mindset, not just a feature. Using GitHub Copilot extensively taught me how AI can boost coding efficiency, while designing a library-like structure sharpened my metadata and integration skills.

## What's next for Knowledge Library MCP

Next, I’ll expand KL MCP to include voice/audio for dictating queries or video/motion for animated workflows, broadening its multi-modal scope. I’ll refine the bot system with smarter domain logic, enhance metadata with AI-driven tagging, and integrate Azure Machine Learning for predictive insights—all while upholding Responsible AI.

---

## New Features and Updates in FinancialAnalysisApp

### New Functionalities in FinancialAnalysisApp/Program.cs

The `FinancialAnalysisApp/Program.cs` file has been updated with new functionalities to enhance the financial analysis capabilities of the application. The new features include:

- **Command-line Argument Processing**: The application now supports command-line arguments for listing and managing vector stores, as well as specifying files for analysis.
- **Environment Variable Support**: The application requires specific environment variables for configuration, such as `AIPROJECT_CONNECTION_STRING` and `USER_PREFIX`.
- **File Analysis**: The application can analyze files with supported extensions and extract relevant information based on the file name format.
- **Vector Store Management**: The application can list, manage, and delete vector stores in Azure.
- **Interactive Chat Loop**: The application includes an interactive chat loop for querying the agent and receiving responses based on the provided file data.

### New Dependencies and Installation Process

The `FinancialAnalysisApp` directory now includes new dependencies that need to be installed for the application to function correctly. The dependencies are specified in the `FinancialAnalysisApp.csproj` file and can be installed using the following command:

```bash
dotnet restore
```

The new dependencies include:

- `Azure.AI.DocumentIntelligence`
- `Azure.AI.Inference`
- `Azure.AI.Projects`
- `Azure.Identity`

### How to Use the New Features

To use the new features implemented in the `FinancialAnalysisApp`, follow these steps:

1. **Set Environment Variables**: Ensure that the required environment variables are set. For example:

```bash
export AIPROJECT_CONNECTION_STRING="your_connection_string"
export USER_PREFIX="your_user_prefix"
```

2. **Run the Application**: Use the following command to run the application with the desired command-line arguments:

```bash
dotnet run <filename>
```

Replace `<filename>` with the name of the file you want to analyze. The filename must follow the format `<ticker>--<form>--<date>_<timestamp>.<extension>`.

3. **List and Manage Vector Stores**: Use the following command to list and manage vector stores:

```bash
dotnet run -l
```

4. **Interactive Chat Loop**: After running the application with a file, you can enter questions to interact with the agent and receive responses based on the file data.

```bash
> What is the revenue for the specified period?
```

5. **Exit the Application**: Press any key to exit the application after completing your queries.

### Use of Azure.AI.DocumentIntelligence for Image Preprocessing

The `FinancialAnalysisApp/Program.cs` file now includes the use of `Azure.AI.DocumentIntelligence` for image preprocessing. This allows the application to extract content from image files and convert it into a format that can be analyzed by the agent. The extracted content is saved as a Markdown file, which is then uploaded and used to create or update vector stores. This enhances the application's ability to handle multi-modal data, including both text and images.

