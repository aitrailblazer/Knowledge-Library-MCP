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