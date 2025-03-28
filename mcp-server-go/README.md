# MCP Server Go

The `mcp-server-go` is a Go-based server designed to implement the Model Context Protocol (MCP) for enabling AI agents to interact with external tools and data sources. This server is a critical component of the Knowledge Library MCP project, providing high-performance and extensible capabilities for AI-driven applications.

## Features
- **MCP Implementation**: Fully compliant with the Model Context Protocol for tool execution and data retrieval.
- **Tool Integration**: Includes tools for web search, financial data retrieval, and more.
- **High Performance**: Built with Go for efficient handling of concurrent requests.
- **Extensibility**: Easily extendable to add new tools and functionalities.
- **Lightweight**: Minimal dependencies for easy deployment and maintenance.

## Prerequisites
- Go 1.20 or later
- API keys for integrated tools (e.g., Brave Search, Yahoo Finance)

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd mcp-server-go
   ```
2. Install dependencies:
   ```bash
   go mod tidy
   ```
3. Run the server:
   ```bash
   go run main.go
   ```

## Usage
1. Start the server:
   ```bash
   go run main.go
   ```
2. Send a POST request to the server with the following JSON payload:
   ```json
   {
       "tool": "tool_name",
       "parameters": {
           "key": "value"
       }
   }
   ```
3. The server will execute the specified tool and return the results in JSON format.

## Files
- `main.go`: Main entry point for the server.
- `tools/`: Contains utility scripts for tool implementations (e.g., `brave_search.go`, `yahoo_finance.go`).
- `go.mod`: Go module dependencies.

## Troubleshooting
- **Tool Errors**: Ensure the tool name and parameters in the request are valid.
- **API Key Issues**: Verify that the API keys for integrated tools are correctly configured.
- **Dependency Issues**: Run `go mod tidy` to ensure all dependencies are installed.

## Technologies Used
- **Go**: The primary programming language used for the server.
- **HTTP/JSON**: For implementing the MCP protocol and handling client-server communication.
- **Go Concurrency**: For efficient handling of multiple requests and tool executions.
- **External APIs**: For integrating tools like Brave Search and Yahoo Finance.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.