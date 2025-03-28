# MCP Azure Server

The `mcp-azure-server` is a Python-based server designed to integrate with Azure AI services, providing advanced capabilities for document processing, vector store management, and AI-powered interactions. It acts as a backend for the Knowledge Library MCP project, enabling seamless communication with Azure services.

## Features
- **Document Processing**: Integrates with Azure Document Intelligence for text extraction and image analysis.
- **Vector Store Management**: Handles embeddings and metadata for semantic search.
- **Real-Time Queries**: Supports real-time AI-powered queries and responses.
- **Authentication**: Uses OAuth 2.0 for secure communication with Azure services.
- **Extensibility**: Easily adaptable to include additional Azure services or APIs.

## Prerequisites
- Python 3.10 or later
- Azure AI Service credentials
- Required Python packages (see `requirements.txt`)

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd mcp-azure-server
   ```
2. Install the required Python packages:
   ```bash
   pip install -r requirements.txt
   ```
3. Configure Azure credentials in the `tokens.json` file or as environment variables.

## Usage
1. Start the server:
   ```bash
   python mcp_server.py
   ```
2. The server will listen for requests on the configured port (default: `http://localhost:5000`).
3. Use the API endpoints to interact with Azure services for document processing and queries.

## Files
- `mcp_server.py`: Main server script.
- `requirements.txt`: Python dependencies for the project.
- `tokens.json`: Stores authentication tokens for Azure services.
- `startup.txt`: Configuration file for server startup parameters.

## Troubleshooting
- **Authentication Errors**: Ensure the `tokens.json` file contains valid credentials or set the required environment variables.
- **Dependency Issues**: Run `pip install -r requirements.txt` to ensure all dependencies are installed.
- **Connection Errors**: Verify that the Azure services are accessible and the network configuration is correct.

## Technologies Used
- **Python**: The primary programming language used for the server.
- **Azure AI Services**: For document processing, vector store management, and AI-powered interactions.
- **OAuth 2.0**: For secure authentication and token management.
- **Flask**: For building the server and handling HTTP requests.
- **JSON**: For structured data exchange between the server and clients.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## How It Works

The MCP Azure Server operates as a middleware between your applications and Azure AI services, providing a unified interface for AI-powered operations. Here's how it works:

1. **Authentication Flow**:
   - The server uses OAuth 2.0 for authentication with Azure services
   - On first run, it initiates the authentication flow via `/auth/start`
   - After successful authentication, tokens are stored in `tokens.json`
   - Automatic token refresh happens in the background

2. **Request Processing**:
   - Incoming requests are validated and routed to appropriate handlers
   - The server maintains session state for real-time interactions
   - Requests are processed asynchronously for better performance

3. **Integration with Azure Services**:
   - Azure AI Projects for agent-based interactions
   - Azure Document Intelligence for document processing
   - Azure Vector Store for semantic search capabilities
   - Microsoft Graph API for file access and management

## Examples

### 1. Adding Numbers Tool
```json
POST /mcp/tools/call
{
    "name": "add_numbers",
    "arguments": {
        "a": 5,
        "b": 3
    }
}

Response:
{
    "content": [
        {
            "type": "text",
            "text": "Sum of 5 and 3 is 8",
            "data": null
        }
    ],
    "isError": false
}
```

### 2. Stock Price Lookup
```json
POST /mcp/tools/call
{
    "name": "get_stock_prices",
    "arguments": {
        "ticker": "TSLA",
        "interval": "1d",
        "period": "1mo"
    }
}

Response:
{
    "content": [
        {
            "type": "json",
            "text": {
                "ticker": "TSLA",
                "interval": "1d",
                "period": "1mo",
                "prices": [
                    {
                        "timestamp": "2025-03-24 16:00:00",
                        "close_price": 198.45
                    },
                    // ... more prices ...
                ]
            }
        }
    ],
    "isError": false
}
```

### 3. OneDrive File Listing
```json
POST /mcp/tools/call
{
    "name": "list_onedrive_root",
    "arguments": {}
}

Response:
{
    "content": [
        {
            "type": "json",
            "data": {
                "items": [
                    {
                        "name": "Documents",
                        "id": "12345...",
                        "type": "folder"
                    },
                    // ... more items ...
                ]
            }
        }
    ],
    "isError": false
}
```

### 4. Natural Language Processing
```json
POST /mcp/process
{
    "text": "What is Tesla's stock price?"
}

Response:
{
    "content": "Based on the latest data, Tesla (TSLA) stock closed at $198.45. Here's a summary of recent price movements..."
}
```

## API Reference

### Authentication Endpoints
- `GET /auth/start`: Initiates the OAuth flow
- `GET /auth/callback`: OAuth callback handler

### MCP Tool Endpoints
- `GET /mcp/tools/list`: Lists available tools
- `POST /mcp/tools/call`: Executes a specific tool
- `POST /mcp/process`: Processes natural language requests

### Health Check
- `GET /health`: Returns server status and connections

## Error Handling

The server provides detailed error messages in a consistent format:

```json
{
    "content": [
        {
            "type": "text",
            "text": "Error description",
            "data": null
        }
    ],
    "isError": true
}
```

Common error scenarios:
- Authentication failures
- Invalid tool parameters
- Service unavailability
- Rate limiting