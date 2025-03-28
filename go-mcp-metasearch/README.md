# go-mcp-metasearch

`go-mcp-metasearch` is a Go-based project designed to provide metasearch functionality by aggregating results from multiple search engines and APIs. It serves as a backend for AI agents, enabling them to fetch and consolidate diverse search results via a standardized interface.

## Features
- **Metasearch Aggregation**: Combines results from multiple search engines and APIs into a unified response.
- **Customizable Search**: Supports filters and parameters for tailored search queries.
- **HTTP-Based**: Listens on `http://localhost:8081` for HTTP POST requests.
- **Efficient**: Leverages Go’s concurrency model for handling multiple API calls and requests efficiently.
- **Extensible**: Easily adaptable to integrate additional search engines or APIs.

## Prerequisites
- Go 1.20 or later
- API keys for integrated search engines (e.g., Brave Search, Bing, etc.)

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd go-mcp-metasearch
   ```
2. Install dependencies:
   ```bash
   go mod tidy
   ```
3. Run the application:
   ```bash
   go run main.go
   ```

## Usage
1. Start the server:
   ```bash
   go run main.go
   ```
2. Send a POST request to `http://localhost:8081` with the following JSON payload:
   ```json
   {
       "query": "search term",
       "filters": {
           "type": "news"
       }
   }
   ```
3. The server will return aggregated search results in JSON format.

## Files
- `main.go`: Main entry point for the application.
- `go.mod`: Go module dependencies.

## Troubleshooting
- **API Errors**: Ensure API keys for integrated search engines are valid and correctly configured.
- **No Results**: Verify that the query and filters are supported by the integrated APIs.
- **Dependencies**: Run `go mod tidy` to ensure all dependencies are installed.

## Technologies Used
- **Go**: The primary programming language used for the project.
- **HTTP/JSON**: For implementing the server and handling client-server communication.
- **Go Concurrency**: For efficient handling of multiple API calls and requests.
- **Search Engine APIs**: For fetching and aggregating search results.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.