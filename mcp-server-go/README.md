# MCP Server (Model Context Protocol)

The MCP server is a Go-based HTTP server that provides a standardized API for executing various tools. It supports tools like BraveSearch for web searches and YahooStockPrice for financial data retrieval.

## Endpoints

### List Available Tools
To list all available tools and their capabilities, use the following `curl` command:

```bash
curl -X GET http:///tools
```

This will return a JSON response containing metadata about the tools.

### Invoke a Tool
To execute a specific tool, use the `/invoke` endpoint with a POST request. For example:

```bash
curl -X POST http://localhost:8080/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "WebTools.BraveSearch", "parameters": {"query": "example search", "search_type": "web"}}'
```

Replace `WebTools.BraveSearch` with the desired tool and provide the appropriate parameters in the JSON payload.

## Supported Tools

### BraveSearch
- **Description**: Perform web, image, video, or news searches using the Brave Search API.
- **Parameters**:
  - `query` (string, required): The search query.
  - `search_type` (string, optional): The type of search (`web`, `image`, `video`, `news`). Defaults to `web`.
  - `api_key` (string, optional): Your Brave API subscription key. If not provided, the server will use the `BRAVE_API_KEY` environment variable.

### YahooStockPrice
- **Description**: Fetch historical stock prices from Yahoo Finance with a specified interval in structured JSON format.
- **Parameters**:
  - `ticker` (string, required): The stock ticker symbol (e.g., `AAPL`, `TSLA`).
  - `interval` (string, required): The interval for historical data (e.g., `1d`, `1wk`, `1mo`).
  - `period` (string, optional): The period for historical data (e.g., `1y`, `5y`).

## Environment Variables

- `BRAVE_API_KEY`: Your Brave API subscription key for accessing BraveSearch.

## Running the Server

To start the MCP server, navigate to the `mcp-server-go` directory and run:

```bash
go run main.go
```

The server will start on `http://localhost:8080` by default.

## Example `curl` Commands

### Web Search
```bash
curl -X POST http://localhost:8080/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "WebTools.BraveSearch", "parameters": {"query": "example search", "search_type": "web"}}'
```

### Image Search
```bash
curl -X POST http://localhost:8080/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "WebTools.BraveSearch", "parameters": {"query": "example search", "search_type": "image"}}'
```

### Video Search
```bash
curl -X POST http://localhost:8080/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "WebTools.BraveSearch", "parameters": {"query": "example search", "search_type": "video"}}'
```

### News Search
```bash
curl -X POST http://localhost:8080/invoke \
  -H "Content-Type: application/json" \
  -d '{"tool": "WebTools.BraveSearch", "parameters": {"query": "example search", "search_type": "news"}}'
```

## Example `curl` Commands with Environment Variables

### Web Search
```bash
curl -s --compressed "https://api.search.brave.com/res/v1/web/search?q=example+search" \
  -H "Accept: application/json" \
  -H "Accept-Encoding: gzip" \
  -H "X-Subscription-Token: $BRAVE_API_KEY"
```

### Image Search
```bash
curl -s --compressed "https://api.search.brave.com/res/v1/images/search?q=example+search&safesearch=strict&count=20&search_lang=en&country=us&spellcheck=1" \
  -H "Accept: application/json" \
  -H "Accept-Encoding: gzip" \
  -H "X-Subscription-Token: $BRAVE_API_KEY"
```

### Video Search
```bash
curl -s --compressed "https://api.search.brave.com/res/v1/videos/search?q=example+search&safesearch=strict&count=20&search_lang=en&country=us&spellcheck=1" \
  -H "Accept: application/json" \
  -H "Accept-Encoding: gzip" \
  -H "X-Subscription-Token: $BRAVE_API_KEY"
```

### News Search
```bash
curl -s --compressed "https://api.search.brave.com/res/v1/news/search?q=Tesla+stock+news&safesearch=strict&count=20&search_lang=en&country=us&spellcheck=1" \
  -H "Accept: application/json" \
  -H "Accept-Encoding: gzip" \
  -H "X-Subscription-Token: $BRAVE_API_KEY"
```