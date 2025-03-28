# MCP Full Server

This is a FastAPI-based implementation of a Model Context Protocol (MCP) server, compliant with the 2024-11-05 specification. The server integrates with Azure AI Agent services and provides dynamic tool registration and execution capabilities.

## Features

- **Dynamic Tool Registration**: Tools can be registered dynamically using decorators.
- **Azure AI Integration**: Supports querying Azure AI Agents for advanced processing.
- **Microsoft Graph Integration**: Enables OneDrive file listing and token-based authentication.
- **RESTful API**: Exposes endpoints for tool management, tool execution, and health checks.
- **Extensibility**: Easily extendable with new tools and capabilities.

## Requirements

- Python 3.8 or higher
- Dependencies listed in `requirements.txt`

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo/your-project.git
   cd your-project/mcp-azure-server
   ```

2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

3. Set up environment variables:
   - `PROJECT_CONNECTION_STRING`: Azure AI Project connection string.
   - `DEFAULT_AGENT_ID`: (Optional) Default agent ID for Azure AI.
   - `MS_GRAPH_CLIENT_ID`, `MS_GRAPH_CLIENT_SECRET`, `MS_GRAPH_TENANT_ID`: Required for Microsoft Graph integration.

   You can use a `.env` file to manage these variables.

## Usage

1. Start the server:
   ```bash
   uvicorn mcp_server:app --host 0.0.0.0 --port 8000
   ```

2. Access the following endpoints:

   - **Health Check**: `GET /health`
     - Returns the server's health status and connectivity information.

   - **List Tools**: `GET /mcp/tools/list`
     - Returns a list of registered tools.

   - **Call Tool**: `POST /mcp/tools/call`
     - Executes a registered tool with the provided arguments.

   - **Process Message**: `POST /mcp/process`
     - Processes a user message and dynamically determines the appropriate tool or response.

   - **Add Numbers**: `POST /mcp/add`
     - Example tool that adds two integers.

   - **Get Stock Prices**: `POST /mcp/stock_prices`
     - Fetches historical stock prices for a given ticker using Yahoo Finance.

   - **List OneDrive Root**: `GET /mcp/tools/call` with `list_onedrive_root`
     - Lists files and folders in the user's OneDrive root directory.

   - **Get Config**: `GET /mcp/config`
     - Returns server configuration details.

   - **Authentication**:
     - `GET /auth/start`: Starts the Microsoft Graph authentication flow.
     - `GET /auth/callback`: Handles the authentication callback and token exchange.

## Listing All Tools

To list all registered tools, you can use the following `curl` command:

```bash
curl -X GET http://localhost:8000/mcp/tools/list
```

This will return a JSON response with the details of all tools currently registered with the MCP server.

## Example

### Add Numbers Tool

1. Register the tool:
   ```python
   @mcp.tool()
   @register_tool(
       name="add_numbers",
       description="Add two integers together",
       input_schema={
           "type": "object",
           "properties": {
               "a": {"type": "integer", "description": "First number"},
               "b": {"type": "integer", "description": "Second number"}
           },
           "required": ["a", "b"]
       }
   )
   async def add_numbers(a: int, b: int, ctx: Context = None) -> str:
       return f"Sum of {a} and {b} is {a + b}"
   ```

2. Call the tool:
   ```bash
   curl -X POST http://localhost:8000/mcp/add -H "Content-Type: application/json" -d '{"a": 5, "b": 10}'
   ```

   Response:
   ```json
   {
       "content": "Sum of 5 and 10 is 15"
   }
   ```

### Get Stock Prices Tool

1. Register the tool:
   ```python
   @mcp.tool()
   @register_tool(
       name="get_stock_prices",
       description="Retrieve historical stock prices for a ticker from Yahoo Finance",
       input_schema={
           "type": "object",
           "properties": {
               "ticker": {"type": "string", "description": "Stock ticker symbol (e.g., AAPL)"},
               "interval": {"type": "string", "description": "Time interval (e.g., 1d, 1h, 1m), default 1d"},
               "period": {"type": "string", "description": "Time period (e.g., 1mo, 1y, 5d), default 1mo"}
           },
           "required": ["ticker"]
       }
   )
   async def get_stock_prices(ticker: str, interval: str = "1d", period: str = "1mo", ctx: Context = None) -> str:
       # Implementation
   ```

2. Call the tool:
   ```bash
   curl -X POST http://localhost:8000/mcp/stock_prices -H "Content-Type: application/json" -d '{"ticker": "AAPL", "interval": "1d", "period": "1mo"}'
   ```

   Response:
   ```json
   {
       "content": {
           "ticker": "AAPL",
           "interval": "1d",
           "period": "1mo",
           "prices": [
               {"timestamp": "2025-03-20 16:00:00", "close_price": 150.25},
               {"timestamp": "2025-03-21 16:00:00", "close_price": 152.30}
           ]
       }
   }
   ```

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Contributing

Contributions are welcome! Please submit a pull request or open an issue for any bugs or feature requests.

## Acknowledgments

- [FastAPI](https://fastapi.tiangolo.com/)
- [Azure AI](https://azure.microsoft.com/en-us/services/cognitive-services/)
- [Model Context Protocol](https://modelcontextprotocol.org/)
- [Yahoo Finance](https://finance.yahoo.com/)