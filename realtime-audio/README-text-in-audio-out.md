# Real-Time Audio Quickstart

The `text-in-audio-out.py` script is a Python-based application that enables real-time audio processing and interaction with Azure OpenAI services. It allows users to speak into their microphone, transcribe their speech, and receive responses in both text and audio formats.

## Features
- **Real-Time Speech-to-Text**: Transcribes user speech in real-time.
- **Text and Audio Responses**: Provides responses in both text and audio formats.
- **Streaming Transcripts**: Displays the transcript of the response as the audio is being played.
- **Microphone Input**: Captures audio input directly from the user's microphone.
- **Speaker Output**: Plays audio responses through the user's speakers.
- **Error Handling**: Handles WebSocket connection errors and retries gracefully.
- **Logging**: Saves input transcripts and responses to a log file for later review.

## Prerequisites
- Python 3.10 or later
- A microphone and speakers connected to your system
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
   python text-in-audio-out.py
   ```

3. Speak into your microphone when prompted. The script will transcribe your speech and provide a response in both text and audio formats.

## How It Works
1. **Audio Input**: The script captures audio input from the microphone in real-time.
2. **Speech Detection**: It detects when the user starts and stops speaking.
3. **Azure OpenAI Interaction**: The audio input is sent to Azure OpenAI services for processing.
4. **Response Streaming**: The response is streamed back in both text and audio formats, with the text displayed as the audio plays.
5. **Logging**: Input transcripts and responses are saved to `response_log.txt` for later review.

## Files
- `text-in-audio-out.py`: Main script for real-time audio processing.
- `requirements.txt`: Python dependencies for the project.
- `captured_audio.wav`: Example audio file (if generated).
- `response_log.txt`: Log file for input transcripts and responses.

## Troubleshooting
- **No Audio Detected**: Ensure your microphone is connected and working. Adjust the `SILENCE_THRESHOLD` in the script if needed.
- **WebSocket Errors**: Check your Azure OpenAI Service credentials and network connection.
- **Dependencies**: Ensure all required Python packages are installed using `pip install -r requirements.txt`.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Text Search Audio

The `text-search-audio.py` script is designed to perform text-based searches and provide audio responses. It integrates text input with audio output capabilities, making it a versatile tool for interactive applications.

### Features
- Accepts text input for search queries.
- Provides audio responses based on the search results.
- Utilizes advanced audio processing for clear and concise output.

### Usage
1. Run the script:
   ```bash
   python text-search-audio.py
   ```
2. Enter your search query when prompted.
3. Listen to the audio response generated by the script.

### Requirements
Ensure you have the necessary dependencies installed. Refer to the `requirements.txt` file for more details.

### Integration
This script can be integrated with other components in the `realtime-audio` folder to enhance functionality and provide a seamless user experience.

## Technologies Used

The `text-in-audio-out.py` script leverages the following technologies:

- **Python**: The primary programming language used for the script.
- **Azure OpenAI Service**: For processing audio input and generating responses in both text and audio formats.
- **SpeechRecognition Library**: For capturing and converting audio input into text.
- **PyDub**: For audio playback and manipulation.
- **WebSocket**: For real-time communication with Azure OpenAI services.
- **Environment Variables**: Managed using a `.env` file for secure storage of credentials.

These technologies enable the script to provide a seamless real-time audio-to-text and text-to-audio interaction experience.

## Usage of GPT-4o Realtime Preview

The `text-in-audio-out.py` script integrates gpt-4o-realtime-preview **GPT-4o Realtime Preview** to enhance its real-time audio processing capabilities. This includes:

- **Real-Time Transcription**: Converts audio input into text with high accuracy and low latency.
- **Text-to-Audio Responses**: Generates audio responses in real-time, providing a seamless conversational experience.
- **Multimodal Interaction**: Combines audio and text processing to support dynamic and interactive use cases.

This integration ensures that the script delivers a robust and interactive experience, leveraging the power of GPT-4o for real-time AI-driven interactions.