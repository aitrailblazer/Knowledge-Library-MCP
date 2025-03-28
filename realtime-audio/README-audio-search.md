# Audio Search

The `audio-search.py` script is a Python-based application designed to process audio input and perform searches or queries using Azure OpenAI services. It allows users to interact with the system via audio and receive responses in text format.

## Features
- **Audio Input**: Captures audio input from the user's microphone.
- **Search and Query**: Processes the audio input to perform searches or queries.
- **Text Responses**: Provides responses in text format based on the audio input.
- **Error Handling**: Handles WebSocket connection errors and retries gracefully.

## Prerequisites
- Python 3.10 or later
- A microphone connected to your system
- Azure OpenAI Service credentials

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd Knowledge-Library-MCP/realtime-audio-quickstart
   ```
2. Install the required Python packages:
   ```bash
   pip install -r requirements.txt
   ```

## Usage
1. Set up your Azure OpenAI Service credentials:
   - Create a `.env` file in the `realtime-audio-quickstart` directory.
   - Add the following environment variables:
     ```env
     AZURE_OPENAI_ENDPOINT=<your-azure-openai-endpoint>
     AZURE_OPENAI_API_KEY=<your-azure-openai-api-key>
     ```

2. Run the script:
   ```bash
   python audio-search.py
   ```

3. Speak into your microphone when prompted. The script will process your speech and provide a text-based response.

## How It Works
1. **Audio Input**: The script captures audio input from the microphone.
2. **Speech Processing**: It processes the audio input and converts it into text.
3. **Query Execution**: The text is used to perform a search or query using Azure OpenAI services.
4. **Response**: The response is displayed in text format.

## Files
- `audio-search.py`: Main script for audio-based search and query processing.
- `requirements.txt`: Python dependencies for the project.

## Troubleshooting
- **No Audio Detected**: Ensure your microphone is connected and working. Adjust the `SILENCE_THRESHOLD` in the script if needed.
- **WebSocket Errors**: Check your Azure OpenAI Service credentials and network connection.
- **Dependencies**: Ensure all required Python packages are installed using `pip install -r requirements.txt`.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Contact
For questions or support, please contact the repository maintainer.

## Technologies Used

The `audio-search.py` script leverages the following technologies:

- **Python**: The primary programming language used for the script.
- **Azure OpenAI Service**: For processing queries and generating responses based on audio input.
- **SpeechRecognition Library**: For capturing and converting audio input into text.
- **WebSocket**: For real-time communication with Azure OpenAI services.
- **Environment Variables**: Managed using a `.env` file for secure storage of credentials.

These technologies enable the script to provide a seamless audio-to-text query experience with robust error handling and real-time processing.

## Usage of GPT-4o Realtime Preview and Brave Search

The `audio-search.py` script integrates the following advanced technologies:

- gpt-4o-realtime-preview **GPT-4o Realtime Preview**: Utilized for real-time transcription of audio input and generating text and audio responses. This enables seamless interaction with the system through natural language queries.
- **Brave Search API**: Used to fetch web search results based on the transcribed audio input. The results are summarized and presented to the user in both text and audio formats.

These integrations ensure that the script provides a robust and interactive experience, combining the power of real-time AI processing with web search capabilities.