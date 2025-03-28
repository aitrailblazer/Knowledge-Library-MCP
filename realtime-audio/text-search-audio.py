import os
import asyncio
import sounddevice as sd
import numpy as np
from openai import AsyncAzureOpenAI
from dotenv import load_dotenv
import websockets.exceptions
import requests
from pynput import keyboard
import base64

# Audio settings (for playback only)
SAMPLE_RATE = 16000

# Brave Search API settings
BRAVE_URL = "https://api.search.brave.com/res/v1/web/search"

async def process_search(client: AsyncAzureOpenAI):
    """Process typed search query with Brave Search and Azure OpenAI TTS."""
    load_dotenv()
    brave_api_key = os.getenv("BRAVE_API_KEY")
    azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
    azure_api_key = os.getenv("AZURE_OPENAI_API_KEY")
    
    if not brave_api_key:
        print("Error: BRAVE_API_KEY not found in .env file.")
        return
    if not azure_endpoint or not azure_api_key:
        print("Error: Azure OpenAI credentials not found in .env file.")
        return

    max_retries = 3

    while True:
        query = input("Enter your search query (or 'quit' to exit): ").strip()
        if query.lower() == 'quit':
            print("Exiting program.")
            break

        print(f"Processing query: {query}")

        try:
            async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
                await connection.session.update(session={
                    "modalities": ["audio", "text"],
                    "instructions": "Output the exact provided input text as both streaming text and audio, without generating additional content or transcribing the audio output."
                })
                print("WebSocket connected.")
                await asyncio.sleep(1)

                # Fetch Brave Search results
                headers = {"Accept": "application/json", "X-Subscription-Token": brave_api_key}
                params = {"q": query, "count": 3, "country": "us", "lang": "en"}
                response = requests.get(BRAVE_URL, headers=headers, params=params)

                if response.status_code == 200:
                    results = response.json()
                    summary = "Here’s a summary of search results for your query:\n"
                    for result in results.get("web", {}).get("results", []):
                        # Remove character limit to get full description
                        desc = result.get("description", "No description available")
                        summary += f"- {result['title']}: {desc}...\n"
                    print(f"Search summary prepared: {summary}")

                    # Send summary to Azure OpenAI for TTS
                    await connection.conversation.item.create(
                        item={
                            "type": "message",
                            "role": "user",
                            "content": [{"type": "input_text", "text": summary}]
                        }
                    )
                    await connection.response.create()

                    response_text = ""
                    audio_buffer = bytearray()
                    stop_response = False

                    def on_press_response(key):
                        nonlocal stop_response
                        if key == keyboard.Key.space:
                            print("\nSPACE pressed. Stopping response and restarting...")
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
                            audio_buffer.extend(base64.b64decode(event.delta))
                        elif event.type == "response.done":
                            print(f"\nYou entered: {query}")
                            if response_text:
                                print(f"Response: {response_text}")
                            else:
                                print("Warning: No text response received from API.")
                                print(f"Response (from Brave Search): {summary}")  # Fallback
                            if audio_buffer:
                                audio_data = np.frombuffer(audio_buffer, dtype='int16')
                                sd.play(audio_data, samplerate=SAMPLE_RATE)
                                sd.wait()
                            else:
                                print("Warning: No audio response received.")
                            print("\nReady for your next query.")
                            break

                    response_listener.stop()
                    if stop_response:
                        continue

                else:
                    print(f"Search error: {response.status_code}, {response.text}")

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

async def main():
    load_dotenv()
    client = AsyncAzureOpenAI(
        azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
        api_key=os.environ["AZURE_OPENAI_API_KEY"],
        api_version="2024-10-01-preview",
    )
    await process_search(client)

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nProgram terminated by user. Goodbye!")