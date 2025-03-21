# Project Story: Knowledge Library MCP – Where Azure AI and GPT-4o Vision Turn Chaos into Financial Fire

## About the Project

Picture a data inferno—thousands of documents blazing with Machine Learning notes, SEC filings like TSLA 10-Ks and 10-Qs, process workflows, and charts lighting up trends like neon signs in the dark. That’s the chaos I conquered with **Knowledge Library MCP (KL MCP)**, a multi-modal powerhouse I built to not just survive but dominate. It’s a lightning-fast system for snagging documents and firing off chat-based queries, powered by Azure AI Agent Service, Microsoft Document AI, and a freshly upgraded FinancialAnalysisApp. Let’s dive into the inspiration, the grind, the lessons, and the wins that made this project a total game-changer.

![Architecture Diagram](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-1-front.png)  
*The library-like backbone of KL MCP, fueled by Azure AI Agent Service, Microsoft Document AI, and my custom MCP bots.*

## Inspiration

As a developer, I was stuck in a documentation nightmare—hours lost digging through PDFs, Excel sheets, and charts just to find a single spark of insight. Anthropic’s Model Context Protocol (MCP) lit a fire in me, showing how context could turn disorganized data into gold. I started by breaking documents into chunks and stashing them in Cosmos DB NoSQL, but as the pile grew, my searches started missing the beat. That frustration became my fuel. I envisioned KL MCP as a library-like fortress, powered by Azure’s AI arsenal and Microsoft Document AI, turning a daily grind into a hackathon triumph that could handle anything I threw at it.

![Initial Chaos](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06.png)  
*The document mess that ignited my quest for a smarter, library-like solution.*

## What It Does

KL MCP is a data rockstar—it hunts down documents, whether they’re text or images, like Machine Learning pipelines, financial filings, or workflow sketches, and lets you grill them with conversational queries. It can tackle a TSLA 10-K chart and answer “What’s the revenue outlook?” or dive into a process flow and suggest “Here’s how to streamline this.” It handles a wild mix of formats—PDFs, Word docs, Excel sheets, PowerPoint slides, raw text, HTML, and images—using Microsoft Document AI’s sharp OCR and layout analysis to make sense of it all. With Azure AI Agent Service and my custom bot system at its core, KL MCP doesn’t just match Azure AI Search—it blows past it, weaving in live data and code-driven insights for a chat experience that’s as dynamic as a live concert.

![KL MCP Demo](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-front.png)  
*KL MCP snags documents and powers chat queries across text and images like a pro.*

## How I Built It

I rolled up my sleeves and built KL MCP solo, leaning on Azure AI Agent Service, Microsoft Document AI, and some serious assist from GitHub Copilot to keep my coding sharp and my logic tighter than a drum. Here’s how I brought this beast to life, broken down into the key moves that made it happen.

### Laying the Foundation with Azure AI Agent Service

Azure AI Agent Service became the heavy hitter at the heart of KL MCP—a fully managed platform that spins up intelligent agents with the muscle of cutting-edge models like GPT-4o and Mistral. It’s loaded with tools for file search and code execution, making it a beast at chewing through text-based data—raw text files, structured spreadsheets, you name it—pulling insights from storage solutions or search indices faster than you can blink. The official docs hype its text-handling chops, positioning it as a leader in data-driven smarts.

But here’s the catch—it’s blind to visuals. A chart like the MSCI market cap breakdown, tracking the US, Europe, and Japan’s percentages of the global MSCI index over a decade, was completely off its radar. For KL MCP, that was a dealbreaker. I needed to extract insights from visuals just as much as text, and Azure AI Agent Service needed a serious upgrade to see the full picture.

### Bringing in the Vision with GPT-4o

That’s where GPT-4o, OpenAI’s advanced multimodal marvel, stepped into the spotlight. Hosted on Azure OpenAI Service, GPT-4o doesn’t just crunch text—it sees with precision. It can scan images, pull out text, and decode graphical data like a pro. I’d caught wind of its magic in a recent write-up that showed it extracting financial trends from visuals, and I knew it was the perfect wingman for Azure AI Agent Service.

GPT-4o could unlock the secrets of my MSCI chart, making it a game-changer for KL MCP. The plan was pure fire: let GPT-4o analyze the chart, extract its key details, and turn them into text that Azure AI Agent Service could jam with, bridging the gap between visual and textual data into a seamless workflow.

### Building the Bot Crew

With that foundation set, I got to work crafting a crew of bots—DocBot for Machine Learning and workflows, SECBot for filings—each reporting to a domain-specific agent, like one for TSLA or ML docs. With Copilot’s help, I set up a system to upload files to vector stores, while Cosmos DB NoSQL kept metadata like file type and date for semantic searches. Microsoft Document AI jumped in to process both text and images, making chat integration feel alive, not like a static library card catalog.

### Adding Live Data and Visual Flair

Copilot guided me to whip up API calls—like pulling fresh stock prices—ensuring my data stayed current, sidestepping the stale limits of traditional retrieval systems. I wrote Python scripts, with Copilot’s nudge, to crunch numbers and spin up visuals—like profit trend graphs—adding a spark to chat responses.

### Tuning the GPT-4o Agent

With Copilot’s wisdom, I tuned a GPT-4o agent to “find files, run code, and chat,” blending multi-modal insights with a nod to Responsible AI principles like fairness and transparency. Copilot also helped me craft a lightweight app for uploading, threading, and querying, using the Azure AI Agents SDK to keep things smooth.

### Cracking the MSCI Chart

A standout moment was tackling the MSCI market cap chart from a TSLA filing. Titled "SELECTED COUNTRIES MSCI MARKET CAP AS A PERCENT OF WORLD MSCI (percent, daily, based on US$)," it showed the US, Europe, and Japan’s market cap stakes from 2016 to 2025. I isolated the chart from the document, like framing a key piece of evidence, and let GPT-4o loose.

It traced the red line for the US, starting high around 65% and settling just above 60% by 2025; the blue line for Europe, sliding from 50% to just over 35%; and the green line for Japan, dropping from a bold 70% to around 45%. It even caught key markers—like the US at 61.9 on March 14, 2024—and turned the whole visual into a text summary. I stashed that summary in a storage spot Azure AI Agent Service could access, letting me ask questions like “What’s Japan’s MSCI share in 2020?” and get a sharp answer without breaking a sweat.

## Challenges I Faced

Scaling was a beast—my early Cosmos DB NoSQL chunking started fumbling as the document count hit the thousands, losing precision like a fading signal. Shaping a library-like structure within Azure’s ecosystem took some serious trial and error, and syncing metadata with vector stores felt like untangling a web of wires. Balancing live data, code outputs, and Responsible AI standards—like keeping things fair and transparent—pushed me to my limits. But with Copilot’s steady hand and some late-night debugging sessions, I kept the fire burning.

## Accomplishments I’m Proud Of

I’m stoked to have built a multi-modal app that flows like a dream—grabbing a TSLA 10-K chart and chatting about its trends in seconds flat. Beating Azure AI Search with my bot-driven system, weaving in live data and code insights, and baking in Responsible AI principles are wins I’ll shout about. Turning a developer’s daily headache into a solution that feels like magic? That’s the kind of victory that hits deep.

![Chat Result](https://raw.githubusercontent.com/aitrailblazer/Knowledge-Library-MCP/refs/heads/main/img/AIEdgePulse06-02.png)  
*KL MCP chatting about a TSLA 10-K chart, mixing text and image insights like a rockstar.*

## What I Learned

This project opened my eyes to the raw power of Azure AI Agent Service and Microsoft Document AI for multi-modal apps—blending text and image processing with advanced language skills is a total game-changer. I learned that hierarchical systems, like my bot setup, outshine flat indexes when scaling up, and Responsible AI isn’t just a checkbox—it’s the heart of the operation. GitHub Copilot became my wingman, speeding up my coding flow, while building a library-like structure sharpened my skills in metadata and integration.

## What’s Next for Knowledge Library MCP

The future’s electric for KL MCP—I’m planning to crank it up with voice and audio for dictating queries, and maybe even video to animate workflows, pushing its multi-modal game to new heights. I’ll fine-tune the bot system with smarter domain logic, level up metadata with AI-driven tagging, and tap into Azure Machine Learning for predictive insights, all while keeping Responsible AI front and center.

## New Features and Updates in FinancialAnalysisApp

The FinancialAnalysisApp powering KL MCP just got a major glow-up, turning it into a financial analysis powerhouse that takes the project to the next level. I’ve supercharged its core to handle the MSCI chart and beyond with some slick new tricks. Now, it can take command-line inputs to manage vector stores or zero in on specific files for analysis, making it a breeze to focus on something like that TSLA 10-K chart. It pulls in environment settings—like connection strings and user prefixes—to keep everything locked in tight.

The app now dives deep into files, sniffing out supported formats and pulling data based on their names, so it knows exactly what it’s dealing with. It’s also got full control over vector stores—listing, managing, or clearing them out in Azure with a few clicks. Best of all, it’s got a conversational vibe, letting you chat with the agent in a loop, asking about the MSCI chart’s trends—like how the US held strong above 60% while Japan slid from 70% to 45%—and getting answers that feel alive. These updates make FinancialAnalysisApp the perfect sidekick for KL MCP, amplifying its ability to turn financial visuals into actionable insights.