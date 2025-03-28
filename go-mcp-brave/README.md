# go-mcp-brave

`go-mcp-brave` is a custom Model Context Protocol (MCP) server written in Go, designed to integrate with the Brave Search API to provide real-time web, news, image, and video search results. It serves as a backend for AI agents, such as those built with Azure AI Agent Service, enabling them to fetch diverse content types via a standardized MCP interface. The server runs on `http://localhost:8080` and supports the `brave_web_search` tool, returning up to three results per category (web, news, images, videos) for a given query.

## Features
- **Multi-Type Search**: Retrieves web, news, image, and video results from Brave Search API in a single request.
- **HTTP-Based**: Listens on `http://localhost:8080` for MCP-compliant HTTP POST requests.
- **Custom Implementation**: Built with pure Go, avoiding dependency issues with existing MCP libraries.
- **Efficient**: Leverages Go’s concurrency model for handling multiple requests efficiently.
- **Extensible**: Easily adaptable to add more tools or result types.

## How It Works

### Overview
`go-mcp-brave` acts as an MCP server that:
1. **Receives MCP Requests**: Accepts HTTP POST requests with a JSON payload specifying the `brave_web_search` tool and a query parameter (e.g., `"AI ethics"`).
2. **Fetches Results**: Calls the Brave Search API endpoints (`/web/search`, `/news/search`, `/images/search`, `/videos/search`) using the provided `BRAVE_API_KEY`.
3. **Processes Results**: Formats up to three results per category into structured JSON arrays, including titles, descriptions, URLs, and additional metadata (e.g., published date for news, duration for videos).
4. **Returns MCP Response**: Sends back a pretty-printed JSON response with the tool name and categorized results.

### Model Context Protocol (MCP) in go-mcp-brave
The Model Context Protocol (MCP) is an open standard developed by Anthropic to enable AI models to dynamically interact with external tools and data sources through a client-server architecture. `go-mcp-brave` implements a custom MCP server tailored for Brave Search integration, providing a simple yet effective interface for AI agents to retrieve multi-type search results.

#### MCP Protocol Overview
- **Purpose**: MCP allows AI agents to offload complex tasks (e.g., searching the web) to external servers, enhancing their capabilities without embedding logic directly in the agent.
- **Communication**: Clients (e.g., AI agents) send HTTP POST requests with a JSON payload to the server, which processes the request and returns a JSON response.
- **Key Components**:
  - **Request**: Specifies a tool name and parameters.
  - **Response**: Returns the tool name and results in a structured format.

#### MCP Request Format
The server accepts MCP requests in the following JSON structure:
```json
{
  "tool": "brave_web_search",
  "parameters": {
    "query": "search term"
  }
}
```

tool: The name of the tool to execute. Currently, go-mcp-brave supports only "brave_web_search".

parameters: A key-value map containing the search query. The query field is required and specifies the search term (e.g., "AI ethics").

```json
{
  "tool_name": "brave_web_search",
  "web_results": [
    {
      "title": "string",
      "description": "string",
      "url": "string"
    }
  ],
  "news_results": [
    {
      "title": "string",
      "description": "string",
      "url": "string",
      "published": "string"
    }
  ],
  "image_results": [
    {
      "title": "string",
      "url": "string",
      "source": "string"
    }
  ],
  "video_results": [
    {
      "title": "string",
      "url": "string",
      "duration": "string",
      "thumbnail": "string"
    }
  ]
}

```

tool_name: Echoes the requested tool name ("brave_web_search").

web_results: Array of up to 3 web search results with title, description, and URL.

news_results: Array of up to 3 news results with title, description, URL, and published date.

image_results: Array of up to 3 image results with title, image URL, and source URL.

video_results: Array of up to 3 video results with title, video URL, duration, and thumbnail URL.

Protocol Workflow
Request Handling: The server listens at http://localhost:8080 for POST requests. It parses the JSON payload to extract the tool and query.

Tool Execution: For "brave_web_search", it queries Brave Search API’s four endpoints (web, news, images, videos) using the provided query.

Result Processing: Results are decoded (e.g., HTML entities like \u003cstrong\u003e become <strong>) and limited to 3 entries per category.

Response Generation: The server constructs a MCPResponse struct and returns it as pretty-printed JSON with 2-space indentation.

Example MCP Interaction
Request:

```bash
curl -X POST -H "Content-Type: application/json" -d '{"tool": "brave_web_search", "parameters": {"query": "AI ethics"}}' http://localhost:8080
```

```json
{
  "tool_name": "brave_web_search",
  "web_results": [
    {
      "title": "What is AI Ethics? | IBM",
      "description": "<strong>AI</strong> <strong>ethics</strong> is a framework that guides data scientists and researchers to build <strong>AI</strong> systems in an <strong>ethical</strong> manner to benefit society as a whole.",
      "url": "https://www.ibm.com/think/topics/ai-ethics"
    }
  ],
  "news_results": [
    {
      "title": "EU Proposes AI Ethics Laws",
      "description": "New transparency rules proposed in March 2025...",
      "url": "https://news.example.com",
      "published": "2025-03-20"
    }
  ],
  "image_results": [
    {
      "title": "AI Ethics Diagram",
      "url": "https://example.com/image.jpg",
      "source": "https://example.com"
    }
  ],
  "video_results": [
    {
      "title": "AI Ethics Explained",
      "url": "https://youtube.com/watch?v=abc123",
      "duration": "5:32",
      "thumbnail": "https://i.ytimg.com/vi/abc123/hqdefault.jpg"
    }
  ]
}
```

Integration with Brave Search API
Endpoints: The server queries four Brave Search API endpoints (/web/search, /news/search, /images/search, /videos/search) using the BRAVE_API_KEY via the X-Subscription-Token header.

Result Parsing: Responses are parsed into custom structs (BraveSearchResponse, BraveNewsResponse, etc.), with HTML entities decoded for readability.

Concurrency: Go’s http.Client handles concurrent API calls efficiently, though currently executed sequentially for simplicity.

Integration with Azure AI Agent Service
Compatibility: The server’s MCP implementation aligns with Azure AI Agent Service’s expectations for tool calls, using a JSON-based request/response format. However, Azure.AI.Projects beta.2 requires manual endpoint configuration or a workaround (e.g., HttpClient in WebSearchApp) to route requests to http://localhost:8080.

Future Support: Once Azure fully supports custom MCP endpoints, agents can send requests directly without additional client-side logic.

Extensibility
Adding Tools: New tools can be added by extending the handleMcpRequest function with a dispatch map, mapping tool names to handler functions.

Custom Parameters: The parameters field can be expanded to include filters (e.g., "type": "news") for selective result types.

Limitations
Single Tool: Currently supports only "brave_web_search". Multi-tool support requires additional handlers.

Error Handling: Errors in fetching non-web results (e.g., news, images, videos) are logged but don’t halt the response, potentially leaving sections empty.

## Technologies Used

The `go-mcp-brave` project leverages the following technologies:

- **Go**: The primary programming language used for building the MCP server.
- **Brave Search API**: For fetching web, news, image, and video search results.
- **HTTP/JSON**: For implementing the MCP protocol and handling client-server communication.
- **Go Concurrency**: For efficient handling of multiple requests and API calls.
- **Azure AI Agent Service**: For integration with AI agents, enabling dynamic tool execution.

These technologies ensure that the `go-mcp-brave` server is robust, efficient, and extensible for various search and AI integration use cases.

