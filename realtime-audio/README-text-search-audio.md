# Text Search Audio

The `text-search-audio.py` script is a Python-based application designed to perform text-based searches and provide audio responses. It integrates text input with audio output capabilities, making it a versatile tool for interactive applications.

## Features
- **Text-Based Search**: Accepts text input for search queries.
- **Audio Responses**: Provides audio responses based on the search results.
- **Web Integration**: Fetches search results from the web using Brave Search API.
- **Real-Time Interaction**: Processes queries and generates responses dynamically.
- **Error Handling**: Handles network and API errors gracefully.

## Prerequisites
- Python 3.10 or later
- Brave Search API credentials
- A microphone and speakers connected to your system

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd Knowledge-Library-MCP/realtime-audio
   ```
2. Install the required Python packages:
   ```bash
   pip install -r requirements.txt
   ```

## Usage
1. Set up your Brave Search API credentials:
   - Create a `.env` file in the `realtime-audio` directory.
   - Add the following environment variables:
     ```env
     BRAVE_SEARCH_API_KEY=<your-brave-search-api-key>
     ```

2. Run the script:
   ```bash
   python text-search-audio.py
   ```

3. Enter your search query when prompted. The script will fetch results and provide an audio response.

## How It Works
1. **Text Input**: The script accepts text input from the user.
2. **Web Search**: It uses the Brave Search API to fetch relevant search results.
3. **Response Generation**: The results are summarized and converted into audio responses.
4. **Audio Playback**: The audio response is played back to the user.

## Files
- `text-search-audio.py`: Main script for text-based search and audio response generation.
- `requirements.txt`: Python dependencies for the project.
- `response_log.txt`: Log file for input queries and responses.

## Troubleshooting
- **No Audio Output**: Ensure your speakers are connected and working. Check the audio playback settings in the script.
- **API Errors**: Verify your Brave Search API credentials and network connection.
- **Dependencies**: Ensure all required Python packages are installed using `pip install -r requirements.txt`.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Technologies Used

The `text-search-audio.py` script leverages the following technologies:

- **Python**: The primary programming language used for the script.
- **Brave Search API**: For fetching web search results based on user queries.
- **PyDub**: For audio playback and manipulation.
- **Environment Variables**: Managed using a `.env` file for secure storage of credentials.

These technologies enable the script to provide a seamless text-to-audio interaction experience.

## Usage of GPT-4o Realtime Preview and Brave Search

The `text-search-audio.py` script integrates the following advanced technologies:

- gpt-4o-realtime-preview **GPT-4o Realtime Preview**: Utilized for real-time transcription of text input and generating audio responses. This enables seamless interaction with the system through natural language queries.
- **Brave Search API**: Used to fetch web search results based on the text input. The results are summarized and presented to the user in audio format.

These integrations ensure that the script provides a robust and interactive experience, combining the power of real-time AI processing with web search capabilities.