# Financial Analysis App

The `FinancialAnalysisApp` is a .NET-based application designed for analyzing financial documents, such as 10-K and 10-Q filings, and generating insights. It integrates with Azure AI services and supports various document formats, including PDFs and Markdown.

## Features
- **Document Analysis**: Processes financial documents like 10-K and 10-Q filings.
- **Table Extraction**: Extracts and processes tables from financial documents.
- **Markdown Conversion**: Converts processed data into Markdown format for easy readability.
- **Azure Integration**: Leverages Azure AI services for advanced document processing.
- **Command-Line Interface**: Supports command-line inputs for managing vector stores and targeting specific files.

## Prerequisites
- .NET 9.0 SDK
- Azure AI Service credentials
- Required dependencies (see `FinancialAnalysisApp.csproj`)

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd Knowledge-Library-MCP/FinancialAnalysisApp
   ```
2. Build the project:
   ```bash
   dotnet build
   ```

## Usage
1. Run the application:
   ```bash
   dotnet run
   ```
2. Provide the required inputs, such as the file path of the financial document to analyze.
3. The application will process the document and generate outputs in Markdown format.

## Files
- `Program.cs`: Main entry point for the application.
- `TSLA--10-K--20250315_100652.pdf`: Example financial document.
- `TSLA--table--20250315_100652_processed.md`: Processed Markdown output.
- `requirements.txt`: Dependencies for the project.

## Troubleshooting
- **Build Errors**: Ensure you have the correct .NET SDK version installed.
- **Azure Integration Issues**: Verify your Azure AI Service credentials and configuration.
- **File Not Found**: Ensure the file path provided as input exists and is accessible.

## Technologies Used
- **C#**: The primary programming language used for the application.
- **.NET 9.0**: Framework for building the application.
- **Azure AI Services**: For document processing and analysis.
- **Markdown Conversion**: For generating human-readable outputs.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Multimodal Capabilities

The `FinancialAnalysisApp` is designed to be multimodal, enabling it to process and analyze various types of data, including:

- **Text**: Extract and analyze information from financial documents such as PDFs and Markdown files.
- **Images**: Process and extract data from images, including charts and tables embedded in financial reports.
- **Live Data**: Integrate live data sources for dynamic financial insights.

This multimodal approach ensures that the application can handle diverse data types, providing a comprehensive and versatile solution for financial analysis.

