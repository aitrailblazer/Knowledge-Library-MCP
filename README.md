# Project Story

## About the Project

**Knowledge Library MCP (KL MCP)** is a multi-modal application I developed to address the overwhelming task of managing thousands of documents—Machine Learning files, SEC filings like TSLA 10-Ks and 10-Qs, process workflows, and charts. It enables rapid document retrieval and chat-based querying powered by Azure AI Agent Service and Microsoft Document AI. Below, I’ll dive into my inspiration, the building process, key lessons, and the challenges I overcame.

![Architecture Diagram](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-1-front.png)  
*The library-like architecture of KL MCP, driven by Azure AI Agent Service, Microsoft Document AI, and MCP bots.*

## Inspiration

As a developer, I was bogged down by documentation chaos—spending hours sifting through PDFs, Excel sheets, and charts to locate critical details. Anthropic’s Model Context Protocol (MCP) sparked my interest, demonstrating how context could unlock insights from disorganized data. My early attempts involved chunking documents and vectorizing them in Cosmos DB NoSQL, but as the volume grew, precision faltered. This frustration inspired me to create KL MCP—a scalable, multi-modal, library-like system leveraging Azure’s AI tools, including Microsoft Document AI, to transform a persistent pain point into a rewarding hackathon project.

![Initial Chaos](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06.png)  
*The documentation mess that fueled my vision for a smarter, library-like solution.*

## What it Does

KL MCP efficiently locates documents—spanning text and images, such as ML pipelines, financial filings, and workflows—and enables conversational queries to extract insights. It processes a wide range of formats, including PDFs, Word, Excel, PowerPoint, text files, HTML, and images (via Microsoft Document AI’s OCR and layout analysis), answering questions like “What’s the revenue outlook?” or “How can this process be optimized?” Powered by Azure AI Agent Service and a custom bot system, KL MCP surpasses Azure AI Search by integrating live data and code-driven insights for a dynamic, chat-ready experience.

![KL MCP Demo](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-front.png)  
*KL MCP retrieves documents and supports chat-based queries across text and images.*

## How I Built It

I developed KL MCP single-handedly, tapping into Azure AI Agent Service, Microsoft Document AI, and GitHub Copilot to streamline coding and logic refinement:

- **Knowledge Tools with MCP Bots:** I designed a hierarchical bot system—DocBot for ML/workflows, SECBot for filings—managed by a domain-specific agent (e.g., TSLA or ML docs). With Copilot’s assistance, I scripted file uploads to vector stores, while Cosmos DB NoSQL stored metadata (e.g., type, date) for semantic search. Microsoft Document AI processed text and images, enabling dynamic chat integration beyond static indexes.
- **Azure Functions:** Copilot helped craft API calls (e.g., stock prices) for real-time data, overcoming RAG’s outdated data limitations.
- **Code Interpreter:** Using Copilot, I wrote Python scripts to analyze data and generate visuals—like profit trends—enhancing chat responses.
- **Azure AI Studio Agent:** With Copilot’s guidance, I configured a GPT-4o agent to “find files, call functions, run code, and chat,” merging multi-modal insights with Responsible AI principles.
- **C# Client:** Copilot optimized my development of a lightweight app for uploading, threading, and querying via the Azure AI Agents SDK.

## Challenges I Faced

Scaling proved the toughest obstacle—my initial Cosmos DB NoSQL chunking lost accuracy with thousands of files. Aligning a library-like structure with Azure’s ecosystem required experimentation, and syncing metadata with vector stores was intricate. Balancing live data, code outputs, and Responsible AI standards (fairness, transparency) tested my limits, but Copilot and iterative debugging kept me on track.

## Accomplishments I’m Proud Of

I take pride in building a multi-modal app that fluidly manages text and images—locating a TSLA 10-K chart and discussing its trends in moments. Outperforming Azure AI Search with my bot-driven approach, integrating live data and code insights, and embedding Responsible AI are standout achievements. Solving a real developer pain point feels incredibly rewarding.

![Chat Result](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-02.png)  
*KL MCP chatting about a TSLA 10-K chart, seamlessly blending text and image insights.*

## What I Learned

I discovered the power of Azure AI Agent Service and Microsoft Document AI for multi-modal applications—combining text and image processing with advanced language capabilities is a game-changer. Hierarchical systems outshine flat indexes for scalability, and Responsible AI is a core philosophy, not an add-on. GitHub Copilot boosted my coding efficiency, while crafting a library-like structure honed my metadata and integration expertise.

## What’s Next for Knowledge Library MCP

Looking ahead, I plan to expand KL MCP with voice/audio for query dictation and video/motion for animated workflows, further enhancing its multi-modal reach. I’ll refine the bot system with advanced domain logic, improve metadata with AI-driven tagging, and incorporate Azure Machine Learning for predictive insights—all while prioritizing Responsible AI.

---

## New Features and Updates in FinancialAnalysisApp

### New Functionalities in FinancialAnalysisApp/Program.cs

The `FinancialAnalysisApp/Program.cs` file has been enhanced to boost its financial analysis capabilities, with key updates including:

- **Command-line Argument Processing:** Now supports arguments for listing/managing vector stores and specifying analysis files.
- **Environment Variable Support:** Requires variables like `AIPROJECT_CONNECTION_STRING` and `USER_PREFIX` for configuration.
- **File Analysis:** Analyzes files with supported extensions, extracting data based on a structured file name format.
- **Vector Store Management:** Enables listing, managing, and deleting vector stores in Azure.
- **Interactive Chat Loop:** Offers a conversational interface to query the agent using file data.

### New Dependencies and Installation Process

The `FinancialAnalysisApp` directory now relies on updated dependencies, listed in `FinancialAnalysisApp.csproj`. Install them with:

```bash
dotnet restore