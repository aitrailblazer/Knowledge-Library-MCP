import os
import base64
import asyncio
import sounddevice as sd
import numpy as np
from openai import AsyncAzureOpenAI
from dotenv import load_dotenv
import websockets.exceptions
import queue
from scipy.io import wavfile
from pynput import keyboard
import requests

# Audio settings
SAMPLE_RATE = 16000
CHANNELS = 1
BLOCK_DURATION = 0.1
SILENCE_THRESHOLD = 500
SILENCE_DURATION = 1.0
MIN_SPEECH_DURATION = 0.3

# Brave Search API settings
BRAVE_URL = "https://api.search.brave.com/res/v1/web/search"

async def audio_input_stream(q: queue.Queue):
    def callback(indata, frames, time, status):
        if status:
            print(f"Audio callback error: {status}")
        q.put(indata.copy())
    stream = sd.InputStream(samplerate=SAMPLE_RATE, channels=CHANNELS, dtype='int16', blocksize=int(SAMPLE_RATE * BLOCK_DURATION), callback=callback)
    with stream:
        while True:
            await asyncio.sleep(0.1)

async def process_audio(client: AsyncAzureOpenAI, audio_queue: queue.Queue):
    load_dotenv()
    brave_api_key = os.getenv("BRAVE_API_KEY")
    if not brave_api_key:
        print("Error: BRAVE_API_KEY not found in .env file.")
        return

    max_retries = 3

    while True:
        print("Listening... Speak into your microphone and press SPACE to stop.")
        audio_buffer = []
        silence_counter = 0
        speech_detected = False
        speech_duration = 0.0
        stop_listening = False
        silence_message_shown = False
        stop_response = False

        def on_press_recording(key):
            nonlocal stop_listening
            if key == keyboard.Key.space:
                print("\nSPACE pressed. Stopping recording and processing...")
                stop_listening = True
                return False

        recording_listener = keyboard.Listener(on_press=on_press_recording)
        recording_listener.start()

        try:
            async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
                await connection.session.update(session={
                    "modalities": ["audio", "text"],
                    "instructions": "Transcribe the user's audio input and wait for further instructions to summarize search results in text and audio."
                })
                print("WebSocket connected.")
                await asyncio.sleep(1)

                # Recording phase
                while not stop_listening:
                    try:
                        audio_chunk = audio_queue.get_nowait()
                        max_amplitude = np.max(np.abs(audio_chunk))
                        if max_amplitude >= SILENCE_THRESHOLD:
                            if not speech_detected:
                                print("Speech detected, recording...")
                            speech_detected = True
                            speech_duration += BLOCK_DURATION
                            audio_buffer.append(audio_chunk)
                            silence_counter = 0
                            silence_message_shown = False
                        elif speech_detected:
                            silence_counter += BLOCK_DURATION
                            audio_buffer.append(audio_chunk)
                        if speech_detected and silence_counter >= SILENCE_DURATION and speech_duration >= MIN_SPEECH_DURATION:
                            if not silence_message_shown:
                                print("\nLong silence detected. Press SPACE to process or keep speaking...")
                                silence_message_shown = True
                            await asyncio.sleep(0.1)
                    except queue.Empty:
                        await asyncio.sleep(0.01)

                recording_listener.stop()

                if audio_buffer and speech_duration >= MIN_SPEECH_DURATION:
                    print("Processing your input...")
                    audio_data = np.concatenate(audio_buffer)
                    audio_base64 = base64.b64encode(audio_data.tobytes()).decode('utf-8')
                    wavfile.write("captured_audio.wav", SAMPLE_RATE, audio_data)

                    # Send audio for transcription
                    await connection.conversation.item.create(
                        item={
                            "type": "message",
                            "role": "user",
                            "content": [{"type": "input_audio", "audio": audio_base64}]
                        }
                    )
                    await connection.response.create()

                    input_transcript = ""
                    brave_query = None

                    # Get transcription
                    async for event in connection:
                        if event.type == "response.audio_transcript.delta":
                            input_transcript += event.delta
                        elif event.type == "response.audio_transcript.done":
                            brave_query = input_transcript.strip()
                            print(f"Recognized query: {brave_query}")
                            break

                    if brave_query:
                        # Fetch Brave Search results
                        headers = {"Accept": "application/json", "X-Subscription-Token": brave_api_key}
                        params = {"q": brave_query, "count": 3, "country": "us", "lang": "en"}
                        response = requests.get(BRAVE_URL, headers=headers, params=params)

                        if response.status_code == 200:
                            results = response.json()
                            summary = "Here’s a summary of search results for your query:\n"
                            for result in results.get("web", {}).get("results", []):
                                summary += f"- {result['title']}: {result['description'][:100]}...\n"
                            print(f"Search summary prepared: {summary}")

                            # Send summary to Azure OpenAI for TTS
                            await connection.conversation.item.create(
                                item={
                                    "type": "message",
                                    "role": "user",
                                    "content": [{"type": "text", "text": summary}]
                                }
                            )
                            await connection.response.create()

                            response_text = ""
                            has_audio_response = False

                            def on_press_response(key):
                                nonlocal stop_response
                                if key == keyboard.Key.space:
                                    print("\nSPACE pressed. Stopping response and restarting listening...")
                                    sd.stop()
                                    stop_response = True
                                    return False

                            response_listener = keyboard.Listener(on_press=on_press_response)
                            response_listener.start()

                            async for event in connection:
                                if stop_response:
                                    break
                                if event.type == "error":
                                    print(f"Error: {getattr(event, 'error', str(event))}")
                                elif event.type == "response.text.delta":
                                    print(event.delta, flush=True, end="")
                                    response_text += event.delta
                                elif event.type == "response.audio.delta":
                                    audio_data = base64.b64decode(event.delta)
                                    sd.play(np.frombuffer(audio_data, dtype='int16'), samplerate=SAMPLE_RATE)
                                    await asyncio.sleep(0.01)
                                    has_audio_response = True
                                elif event.type == "response.done":
                                    print(f"\nYou said: {input_transcript}")
                                    print(f"Response: {response_text}")
                                    if not response_text or not has_audio_response:
                                        print("Warning: Incomplete response.")
                                    print("\nListening again... Speak into your microphone and press SPACE to stop.")
                                    break

                            response_listener.stop()
                            if stop_response:
                                continue
                        else:
                            print(f"Search error: {response.status_code}, {response.text}")
                    else:
                        print("No valid query transcribed.")

                else:
                    print("\nNo sufficient audio recorded. Listening again...")

        except websockets.exceptions.ConnectionClosedError as e:
            print(f"WebSocket closed unexpectedly: {e}")
            max_retries -= 1
            if max_retries > 0:
                print(f"Retrying ({max_retries} attempts left)...")
                await asyncio.sleep(5)
            else:
                print("Max retries reached. Exiting.")
                break
        except Exception as e:
            print(f"Unexpected error: {e}")
            break
        finally:
            recording_listener.stop()
            stop_listening = False

async def main():
    load_dotenv()
    client = AsyncAzureOpenAI(
        azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
        api_key=os.environ["AZURE_OPENAI_API_KEY"],
        api_version="2024-10-01-preview",
    )
    audio_queue = queue.Queue()
    try:
        await asyncio.gather(audio_input_stream(audio_queue), process_audio(client, audio_queue))
    except KeyboardInterrupt:
        print("\nStopped by user.")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nProgram terminated by user. Goodbye!")