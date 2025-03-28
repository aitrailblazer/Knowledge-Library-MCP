# Knowledge Library MCP

Welcome to the Knowledge Library MCP repository! This repository contains various projects and tools for financial analysis, company research, real-time audio processing, and more. Below is an overview of the structure and purpose of each folder.

## Repository Structure

### 1. `CompanyResearch/`
This folder contains tools and scripts for conducting company research. Key files include:
- `CompanyResearch.py`: Main script for processing company data.
- `doctags.txt`: Tags used for document classification.
- `output.md`: Markdown output of processed data.
- `requirements.txt`: Python dependencies for the project.

### 2. `FinancialAnalysisApp/`
A .NET application for financial analysis. Key files include:
- `Program.cs`: Main entry point for the application.
- `TSLA--10-K--20250315_100652.pdf`: Example financial document.
- `bin/` and `obj/`: Build and output directories.

### 3. `go-mcp-brave/`
A Go-based project for interacting with the Brave search engine. Key files include:
- `main.go`: Main entry point for the application.
- `go.mod`: Go module dependencies.

### 4. `go-mcp-metasearch/`
A Go-based project for metasearch functionality. Key files include:
- `main.go`: Main entry point for the application.
- `go.mod`: Go module dependencies.

### 5. `mcp-azure-server/`
A Python-based server for Azure integration. Key files include:
- `mcp_server.py`: Main server script.
- `requirements.txt`: Python dependencies for the project.
- `tokens.json`: Authentication tokens.

### 6. `mcp-server-go/`
A Go-based server for MCP functionality. Key files include:
- `main.go`: Main entry point for the server.
- `tools/`: Contains utility scripts like `brave_search.go` and `yahoo_finance.go`.

### 7. `realtime-audio-quickstart/`
A Python project for real-time audio processing. Key files include:
- `text-in-audio-out.py`: Script for processing audio input and generating responses.
- `captured_audio.wav`: Example audio file.
- `response_log.txt`: Log of responses.
- `requirements.txt`: Python dependencies for the project.

### 8. `WebSearchApp/`
A placeholder for a web search application. Contains build (`bin/`) and object (`obj/`) directories.

## Getting Started

### Prerequisites
- Python 3.10 or later
- .NET 9.0 SDK
- Go 1.20 or later
- Required Python packages (see `requirements.txt` in respective folders)

### Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd Knowledge-Library-MCP
   ```
2. Install dependencies for Python projects:
   ```bash
   pip install -r <project-folder>/requirements.txt
   ```
3. Build .NET projects:
   ```bash
   dotnet build <project-folder>
   ```
4. Run Go projects:
   ```bash
   go run <project-folder>/main.go
   ```

## Usage
Refer to the `README.md` or `StepByStepGuide.md` in each project folder for detailed instructions on how to use the tools and applications.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This repository is licensed under the MIT License. See the `LICENSE` file for more details.

## Contact
For questions or support, please contact the repository maintainer.

# Project Story: Knowledge Library MCP – Where Azure AI and GPT-4o Vision Turn Chaos into Financial Fire

## About the Project

Picture a data inferno—thousands of documents blazing with Machine Learning notes, SEC filings like TSLA 10-Ks and 10-Qs, process workflows, and charts lighting up trends like neon signs in the dark. That’s the chaos I conquered with **Knowledge Library MCP (KL MCP)**, a multi-modal powerhouse I built to not just survive but dominate. It’s a lightning-fast system for snagging documents from OneDrive, firing off chat-based queries, and pulling live financial insights, powered by Azure AI Agent Service, Microsoft Document AI, and a turbocharged FinancialAnalysisApp. With persistent Microsoft Graph authentication and a bot crew that turns data into gold, this project’s a game-changer. Let’s dive into the inspiration, the grind, the lessons, and the wins that made it roar.

![Architecture Diagram](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-1-front.png)  
*The library-like backbone of KL MCP, fueled by Azure AI Agent Service, Microsoft Document AI, Microsoft Graph, and my custom MCP bots.*

## Inspiration

As a developer, I was drowning in a documentation nightmare—hours lost digging through PDFs, Excel sheets, OneDrive folders, and charts just to spark an insight. Anthropic’s Model Context Protocol (MCP) lit the fuse, showing how context could turn chaos into clarity. I started chunking documents into Cosmos DB NoSQL, but as the pile grew, searches faltered. Microsoft’s Graph API caught my eye—why not tap into OneDrive directly? That frustration became my fuel. I envisioned KL MCP as a fortress-like library, powered by Azure’s AI arsenal, Microsoft Document AI, and Graph API’s seamless access, turning a daily slog into a hackathon triumph that could handle anything—text, images, or live data.

![Initial Chaos](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06.png)  
*The document mess that ignited my quest for a smarter, library-like solution.*

## What It Does

KL MCP is a data rockstar—it hunts down documents from OneDrive, whether they’re text or images, like Machine Learning pipelines, financial filings, or workflow sketches, and lets you grill them with conversational queries. Ask it about a TSLA 10-K chart—“What’s the revenue outlook?”—and it delivers. Toss in a process flow—“How do I streamline this?”—and it’s got your back. It handles PDFs, Word docs, Excel sheets, PowerPoint slides, raw text, HTML, and images, using Microsoft Document AI’s sharp OCR and layout analysis, backed by Azure AI Agent Service and my custom bot system. With Graph API integration, it persists authentication across restarts, pulling live OneDrive data without breaking a sweat. It doesn’t just match Azure AI Search—it smokes it, weaving in live data, code-driven insights, and a chat experience that’s as dynamic as a live gig.

![KL MCP Demo](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-front.png)  
*KL MCP snags OneDrive docs and powers chat queries across text and images like a pro.*

## How I Built It

I rolled up my sleeves and built KL MCP solo, leaning on Azure AI Agent Service, Microsoft Document AI, Microsoft Graph API, and GitHub Copilot to keep my code razor-sharp. Here’s how I turned this vision into reality, step by gritty step.

### Laying the Foundation with Azure AI Agent Service

Azure AI Agent Service became KL MCP’s heavy hitter—a managed platform spinning up agents with GPT-4o and Mistral muscle. It’s a beast at chewing through text—raw files, spreadsheets, you name it—pulling insights from storage or search indices in a flash. The docs tout its text-handling prowess, and it’s a champ for data-driven smarts.

But it’s blind to visuals out of the box. A chart like the MSCI market cap breakdown was invisible to it—unacceptable for KL MCP. I needed a system that could see and think across all data types.

### Bringing in Vision with GPT-4o

Enter GPT-4o, OpenAI’s multimodal marvel on Azure OpenAI Service. It doesn’t just read—it sees. It scans images, extracts text, and decodes charts like a pro. A write-up showed it nailing financial trends from visuals, and I knew it’d supercharge Azure AI Agent Service. GPT-4o could crack my MSCI chart, turning pixels into text that the agents could jam with, bridging visuals and text into a killer workflow.

### Tapping OneDrive with Microsoft Graph API

To level up, I wired in Microsoft Graph API with a confidential client flow using `client_secret`. It grabs docs straight from OneDrive, persisting tokens across restarts with a `tokens.json` file. If the access token expires, it refreshes it automatically using the `refresh_token`, keeping the system humming without constant logins. Copilot helped me nail the OAuth dance—authentication at `/auth/start`, token exchange at `/auth/callback`, and a `list_onedrive_root` tool to fetch and chat about OneDrive goodies.

### Building the Bot Crew

I crafted a bot squad—DocBot for ML and workflows, SECBot for filings—each tied to domain-specific agents (TSLA, ML docs). Copilot guided me to vectorize uploads into stores, while Cosmos DB NoSQL tracked metadata like file type and date. Microsoft Document AI processed text and images, and Graph API pulled OneDrive data, making chats feel alive, not like a dusty catalog.

### Adding Live Data and Visual Flair

Copilot helped me whip up API calls—like live stock prices—keeping data fresh, not stale. Python scripts crunched numbers and spun up visuals—like profit trend graphs—adding flair to chat responses.

### Tuning the GPT-4o Agent

With Copilot’s nudge, I tuned a GPT-4o agent to “find files, run code, and chat,” blending multi-modal insights with Responsible AI principles like fairness and transparency. It integrates OneDrive queries seamlessly, keeping the system sharp.

### Cracking the MSCI Chart

A highlight was tackling the MSCI market cap chart from a TSLA filing—“SELECTED COUNTRIES MSCI MARKET CAP AS A PERCENT OF WORLD MSCI (percent, daily, based on US$).” GPT-4o traced the US red line from 65% to just above 60% by 2025, Europe’s blue line from 50% to over 35%, and Japan’s green line from 70% to around 45%. It caught the US at 61.9 on March 14, 2024, and turned it into text Azure AI Agent Service could use, letting me ask “What’s Japan’s MSCI share in 2020?” and get a crisp answer.

## Challenges I Faced

Scaling was brutal—Cosmos DB chunking faltered at thousands of docs, losing precision. Wiring Graph API with persistent tokens took some sweat, syncing OneDrive metadata with vector stores was a tangle, and balancing live data, code outputs, and Responsible AI standards pushed me hard. Copilot and late-night debugging kept me in the game.

## Accomplishments I’m Proud Of

I’m pumped to have built a multi-modal app that flows like magic—grabbing a TSLA 10-K chart from OneDrive and chatting trends in seconds. Outpacing Azure AI Search with bots, live data, and Graph API persistence, all while honoring Responsible AI, is a win I’ll roar about. Turning a developer’s headache into a solution that’s pure fire? That’s the stuff.

![Chat Result](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-02.png)  
*KL MCP chatting about a TSLA 10-K chart, mixing OneDrive data, text, and images like a rockstar.*

## What I Learned

This project taught me the raw power of Azure AI Agent Service, Microsoft Document AI, and Graph API for multi-modal apps—blending text, images, and live data is next-level. Hierarchical bot systems beat flat indexes at scale, persistent auth via Graph API is a lifesaver, and Responsible AI is the core. Copilot turbocharged my coding, and OneDrive integration sharpened my metadata game.

## What’s Next for Knowledge Library MCP

KL MCP’s future is electric—voice and audio for queries, video to animate workflows, pushing multi-modal limits. I’ll tune bots with smarter logic, boost metadata with AI tagging, and tap Azure Machine Learning for predictions, all while keeping Responsible AI front and center.

## New Features and Updates in FinancialAnalysisApp

The **FinancialAnalysisApp** powering KL MCP got a major upgrade, turning it into a financial analysis beast that elevates the project. It now handles command-line inputs to manage vector stores or target files like that MSCI chart, pulling environment settings for seamless operation. It snags OneDrive docs via Graph API, persisting tokens across restarts, and refreshes them on the fly. It dives into files by name, controls vector stores in Azure, and chats in a loop—ask about the MSCI chart’s US trend holding above 60%, and it delivers live, sharp answers. These updates make FinancialAnalysisApp the ultimate sidekick for KL MCP, turning financial chaos into actionable fire.