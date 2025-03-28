# Knowledge Library MCP

Welcome to the Knowledge Library MCP repository! This repository contains various projects and tools for financial analysis, company research, real-time audio processing, and more. Below is an overview of the structure and purpose of each folder.

## Repository Structure

### 1. `CompanyResearch/`
This folder contains tools and scripts for conducting company research. Key files include:
- `CompanyResearch.py`: Main script for processing company data.
- `doctags.txt`: Tags used for document classification.
- `output.md`: Markdown output of processed data.
- `requirements.txt`: Python dependencies for the project.
- `temp_screenshot.png`: Temporary screenshot of the webpage.
- `website_output.json`: JSON output of structured data.

### 2. `FinancialAnalysisApp/`
A .NET application for financial analysis. Key files include:
- `Program.cs`: Main entry point for the application.
- `TSLA--10-K--20250315_100652.pdf`: Example financial document.
- `TSLA--Q4--2024-Update.pdf`: Quarterly update document.
- `MCRS--OwnershipInformation-KangAndrew--20250311.pdf`: Example ownership information document.
- `bin/` and `obj/`: Build and output directories.

### 3. `go-mcp-brave/`
A Go-based project for interacting with the Brave search engine. Key files include:
- `main.go`: Main entry point for the application.
- `go.mod`: Go module dependencies.

### 4. `go-mcp-metasearch/`
A Go-based project for metasearch functionality. Key files include:
- `main.go`: Main entry point for the application.
- `go.mod`: Go module dependencies.

### 5. `img/`
Contains images used in documentation and architecture diagrams. Key files include:
- `KnowledgeLibraryMCP-Architecture.png`: Architecture diagram for the project.
- `AIEdgePulse06.png`: Example image used in documentation.

### 6. `KnowledgeLibraryMCP/`
The main project folder containing subprojects and scripts. Key files include:
- `architecture.mmd`: Mermaid.js file for generating architecture diagrams.
- `update_architecture_diagram.sh`: Script for updating architecture diagrams.

### 7. `mcp-azure-server/`
A Python-based server for Azure integration. Key files include:
- `mcp_server.py`: Main server script.
- `requirements.txt`: Python dependencies for the project.
- `tokens.json`: Authentication tokens.

### 8. `mcp-server-go/`
A Go-based server for MCP functionality. Key files include:
- `main.go`: Main entry point for the server.
- `tools/`: Contains utility scripts like `brave_search.go` and `yahoo_finance.go`.

### 9. `PDF/`
Contains example PDF files and processed outputs. Key files include:
- `TSLA--10-K--20250315--100652.pdf`: Example financial document.
- `MSFT--10-K--20250327--134736.pdf`: Microsoft 10-K filing.
- `TSLA--table--20250315_100652_processed.md`: Processed Markdown output.

### 10. `realtime-audio/`
A Python project for real-time audio processing. Key files include:
- `audio-search.py`: Script for processing audio input and generating responses.
- `text-in-audio-out.py`: Script for real-time audio interaction.
- `text-search-audio.py`: Script for text-based searches with audio responses.
- `requirements.txt`: Python dependencies for the project.

### 11. `WebSearchApp/`
A placeholder for a web search application. Key files include:
- `bin/` and `obj/`: Build and output directories.

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
