import os
import base64
import asyncio
import sounddevice as sd
import numpy as np
from openai import AsyncAzureOpenAI
from dotenv import load_dotenv
import websockets.exceptions
import queue
from scipy.io import wavfile  # For saving audio
from pynput import keyboard

# Audio settings
SAMPLE_RATE = 16000
CHANNELS = 1
BLOCK_DURATION = 0.1  # 100ms blocks
SILENCE_THRESHOLD = 500  # Adjust based on mic sensitivity
SILENCE_DURATION = 1.0  # Seconds of silence
MIN_SPEECH_DURATION = 0.3  # For short inputs like "hello"

async def audio_input_stream(q: queue.Queue):
    """Stream audio from microphone into a queue."""
    def callback(indata, frames, time, status):
        if status:
            print(f"Audio callback error: {status}")
        q.put(indata.copy())

    stream = sd.InputStream(
        samplerate=SAMPLE_RATE,
        channels=CHANNELS,
        dtype='int16',
        blocksize=int(SAMPLE_RATE * BLOCK_DURATION),
        callback=callback
    )
    with stream:
        while True:
            await asyncio.sleep(0.1)

async def process_audio(client: AsyncAzureOpenAI, audio_queue: queue.Queue):
    """Process audio and interact with the model, stopping recording with SPACE."""
    max_retries = 3

    while True:
        print("Listening... Speak into your microphone and press SPACE to stop.")
        audio_buffer = []
        silence_counter = 0
        speech_detected = False
        speech_duration = 0.0
        stop_listening = False
        silence_message_shown = False  # Flag to prevent repeated logging

        # Define key press handler
        def on_press(key):
            nonlocal stop_listening
            if key == keyboard.Key.space:
                print("\nSPACE pressed. Stopping recording and processing...")
                stop_listening = True
                return False  # Stop listener

        # Start keyboard listener
        listener = keyboard.Listener(on_press=on_press)
        listener.start()

        try:
            async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
                await connection.session.update(session={
                    "modalities": ["audio", "text"],
                    "instructions": "Transcribe the user's audio input and respond with a short greeting in both text and audio formats."
                })
                print("WebSocket connected.")
                await asyncio.sleep(1)  # Stabilize connection

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
                            silence_message_shown = False  # Reset silence message flag
                        elif speech_detected:
                            silence_counter += BLOCK_DURATION
                            audio_buffer.append(audio_chunk)

                        # Check for long silence, but only print message once
                        if speech_detected and silence_counter >= SILENCE_DURATION and speech_duration >= MIN_SPEECH_DURATION:
                            if not silence_message_shown:
                                print("\nLong silence detected. Press SPACE to process or keep speaking...")
                                silence_message_shown = True
                            # Wait for SPACE without flooding logs
                            await asyncio.sleep(0.1)

                    except queue.Empty:
                        await asyncio.sleep(0.01)

                # Process audio if there’s sufficient input
                if audio_buffer and speech_duration >= MIN_SPEECH_DURATION:
                    print("Processing your input...")
                    audio_data = np.concatenate(audio_buffer)
                    audio_base64 = base64.b64encode(audio_data.tobytes()).decode('utf-8')

                    # Optional: Save audio for debugging
                    wavfile.write("captured_audio.wav", SAMPLE_RATE, audio_data)

                    await connection.conversation.item.create(
                        item={
                            "type": "message",
                            "role": "user",
                            "content": [{"type": "input_audio", "audio": audio_base64}],
                        }
                    )
                    await connection.response.create()

                    input_transcript = ""
                    response_text = ""
                    has_audio_response = False

                    async for event in connection:
                        if event.type == "error":
                            error_details = f"Error event - Type: {event.type}"
                            if hasattr(event, 'error'):
                                error_details += f", Details: {event.error}"
                            elif hasattr(event, 'message'):
                                error_details += f", Message: {event.message}"
                            else:
                                error_details += f", Raw: {str(event)}"
                            print(error_details)
                        elif event.type == "response.audio_transcript.delta":
                            input_transcript += event.delta
                        elif event.type == "response.text.delta":
                            print(event.delta, flush=True, end="")  # Stream text
                            response_text += event.delta
                        elif event.type == "response.audio.delta":
                            audio_data = base64.b64decode(event.delta)
                            sd.play(np.frombuffer(audio_data, dtype='int16'), samplerate=SAMPLE_RATE)
                            sd.wait()  # Play audio
                            has_audio_response = True
                        elif event.type == "response.done":
                            print(f"\nYou said: {input_transcript}")
                            print(f"Response: {response_text}")
                            if not response_text or not has_audio_response:
                                print("Warning: Incomplete response received.")
                                await connection.response.create()  # Retry
                            with open("response_log.txt", "a") as log_file:
                                log_file.write(f"Input Transcript: {input_transcript}\n")
                                log_file.write(f"Response Text: {response_text}\n\n")
                            print("\nListening again... Speak into your microphone and press SPACE to stop.")
                            break

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
            listener.stop()  # Clean up listener
            stop_listening = False  # Reset for next iteration

async def main():
    """Initialize and run the program."""
    load_dotenv()
    client = AsyncAzureOpenAI(
        azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
        api_key=os.environ["AZURE_OPENAI_API_KEY"],
        api_version="2024-10-01-preview",
    )
    audio_queue = queue.Queue()
    try:
        await asyncio.gather(
            audio_input_stream(audio_queue),
            process_audio(client, audio_queue)
        )
    except KeyboardInterrupt:
        print("\nStopped by user.")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nProgram terminated by user. Goodbye!")