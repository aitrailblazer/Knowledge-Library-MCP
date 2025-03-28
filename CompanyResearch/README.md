# Company Research

The `CompanyResearch` project is a Python-based application designed to extract and process structured data from webpages. It leverages advanced tools like Playwright, Docling-Core, and MLX-VLM to convert webpage content into structured formats such as JSON and Markdown.

## Features
- **Webpage to Image Conversion**: Captures full-page screenshots of webpages using Playwright.
- **Image and HTML Processing**: Extracts structured data, including text, headings, tables, and images, from webpage screenshots and HTML content.
- **DocTags Generation**: Uses MLX-VLM and Docling-Core to generate DocTags for comprehensive document representation.
- **Fallback Mechanism**: Extracts structured text directly from HTML as a fallback.
- **Output Formats**: Saves structured data in JSON and Markdown formats.

## Prerequisites
- Python 3.10 or later
- Required Python packages (see `requirements.txt`)
- Playwright installed and configured

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd Knowledge-Library-MCP/CompanyResearch
   ```
2. Install the required Python packages:
   ```bash
   pip install -r requirements.txt
   ```
3. Install Playwright:
   ```bash
   playwright install
   ```

## Usage
1. Set the target URL in the `main()` function of `CompanyResearch.py`:
   ```python
   url = "https://example.com"
   ```
2. Run the script:
   ```bash
   python CompanyResearch.py
   ```
3. The script will:
   - Capture a screenshot of the webpage.
   - Extract structured data using Docling-Core and MLX-VLM.
   - Save the output in `website_output.json` and `output.md`.

## Files
- `CompanyResearch.py`: Main script for processing webpages.
- `doctags.txt`: File for storing raw DocTags.
- `output.md`: Markdown output of processed data.
- `website_output.json`: JSON output of structured data.
- `requirements.txt`: Python dependencies for the project.
- `temp_screenshot.png`: Temporary screenshot of the webpage.

## Troubleshooting
- **Playwright Errors**: Ensure Playwright is installed and configured correctly. Run `playwright install` if needed.
- **Timeouts**: Increase the timeout values in the `webpage_to_image` function if the webpage takes longer to load.
- **Model Loading Issues**: Verify the model path and dependencies for MLX-VLM and Docling-Core.

## Technologies Used
- **Python**: The primary programming language used for the script.
- **Playwright**: For capturing webpage screenshots and interacting with webpages.
- **Docling-Core**: For processing and structuring document data.
- **MLX-VLM**: For generating DocTags and extracting structured data.
- **BeautifulSoup**: For fallback HTML parsing and text extraction.
- **Pillow**: For image processing and saving screenshots.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.