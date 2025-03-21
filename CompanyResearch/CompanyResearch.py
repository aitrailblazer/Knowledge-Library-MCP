from playwright.sync_api import sync_playwright, TimeoutError
from io import BytesIO
from pathlib import Path
from PIL import Image
import json
import time

from docling_core.types.doc import ImageRefMode
from docling_core.types.doc.document import DocTagsDocument, DoclingDocument
from mlx_vlm import load, generate
from mlx_vlm.prompt_utils import apply_chat_template
from mlx_vlm.utils import load_config, stream_generate
from bs4 import BeautifulSoup

# Settings
OUTPUT_FILE = "website_output.json"
DOCTAGS_FILE = "doctags.txt"  # File for raw DocTags
TEMP_IMAGE = "webpage_screenshot.png"  # Image file for webpage screenshot

# Step 1: Convert webpage to image with Playwright
def webpage_to_image(url):
    max_retries = 3
    for attempt in range(max_retries):
        try:
            print(f"Converting webpage {url} to image (Attempt {attempt + 1}/{max_retries})...")
            with sync_playwright() as p:
                browser = p.chromium.launch(headless=True)
                page = browser.new_page()
                page.set_extra_http_headers({
                    "User-Agent": "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)"
                })
                response = page.goto(url, timeout=60000)
                if response.status != 200:
                    raise Exception(f"Failed with status code: {response.status}")
                page.wait_for_load_state("networkidle", timeout=30000)
                # Set viewport to capture full page (adjust as needed)
                page.set_viewport_size({"width": 1920, "height": 1080})
                content = page.content()
                images = [{"src": img.get_attribute('src'), "alt": img.get_attribute('alt') or ""} 
                          for img in page.query_selector_all('img') if img.get_attribute('src') and "data:image" not in img.get_attribute('src')]
                # Capture screenshot of full page
                screenshot_bytes = page.screenshot(full_page=True)
                pil_image = Image.open(BytesIO(screenshot_bytes))
                pil_image.save(TEMP_IMAGE, 'PNG')
                print(f"Successfully converted webpage to image ({TEMP_IMAGE}, {len(images)} additional images fetched)")
                browser.close()
                return {"html": content, "images": images, "screenshot": pil_image}
        except TimeoutError:
            print(f"Request timed out after 60 seconds or network idle wait failed for {url}")
            if attempt < max_retries - 1:
                print("Retrying after 5-second delay...")
                time.sleep(5)
            else:
                print("Max retries reached. Giving up.")
                return None
        except Exception as e:
            print(f"Error converting webpage to image: {e}")
            if attempt < max_retries - 1:
                print("Retrying after 5-second delay...")
                time.sleep(5)
            else:
                print("Max retries reached. Giving up.")
                return None

# Step 2: Process image with Docling-Core and MLX-VLM
def process_with_docling_core(fetch_result, url, output_file=OUTPUT_FILE, doctags_file=DOCTAGS_FILE):
    html_content = fetch_result["html"]
    images = fetch_result["images"]
    screenshot = fetch_result["screenshot"]

    # Load the model
    model_path = "ds4sd/SmolDocling-256M-preview-mlx-bf16"
    try:
        model, processor = load(model_path)
        config = load_config(model_path)
    except Exception as e:
        print(f"Failed to load model: {e}")
        return None

    # Enhanced prompt for full page reconstruction
    prompt = "Extract all unique text, headings, paragraphs, lists, tables, and images with their structure, hierarchy, and detailed content from this webpage image into comprehensive DocTags, avoiding repetition and ensuring full page coverage."

    # Apply chat template
    formatted_prompt = apply_chat_template(processor, config, prompt, num_images=1)

    # Generate and save DocTags
    print("Generating DocTags from webpage image...\n")
    output = ""
    seen_tags = set()  # Deduplicate DocTags
    for token in stream_generate(
        model, processor, formatted_prompt, [screenshot], max_tokens=16384, verbose=False
    ):
        tag_content = token.text.strip()
        if tag_content and tag_content not in seen_tags:
            output += tag_content
            seen_tags.add(tag_content)
        if "</doctag>" in token.text:
            break
    print("Raw DocTags output:", output)

    # Save raw DocTags to file first
    with open(doctags_file, 'w', encoding='utf-8') as f:
        f.write(output)
    print(f"Raw DocTags saved to: {doctags_file}")

    # Populate document from saved DocTags
    doctags_doc = DocTagsDocument.from_doctags_and_image_pairs([output], [screenshot])
    doc = DoclingDocument(name="WebsiteDocument")
    doc.load_from_doctags(doctags_doc)

    # Debug Docling document state
    print("DoclingDocument attributes:", dir(doc))
    print("Main text:", getattr(doc, 'main_text', 'None'))
    print("Tables:", getattr(doc, 'tables', 'None'))

    # Fallback: Extract unique structured text from HTML
    soup = BeautifulSoup(html_content, 'html.parser')
    fallback_lines = []
    seen_texts = set()  # Deduplicate text
    for tag in soup.find_all(['h1', 'h2', 'h3', 'p', 'li', 'div', 'span']):
        text = tag.get_text(strip=True)
        if text and text not in seen_texts:
            seen_texts.add(text)
            if tag.name == 'h1':
                fallback_lines.append(f"# {text}")
            elif tag.name == 'h2':
                fallback_lines.append(f"## {text}")
            elif tag.name == 'h3':
                fallback_lines.append(f"### {text}")
            elif tag.name == 'li':
                fallback_lines.append(f"- {text}")
            else:
                fallback_lines.append(text)
    fallback_markdown = "\n\n".join(fallback_lines)

    # Structured data
    structured_data = {
        "document": {
            "title": "Website Data",
            "content": [],
            "markdown": doc.export_to_markdown() if doc.export_to_markdown() else fallback_markdown,
            "images": images + [{"src": TEMP_IMAGE, "alt": "Page Screenshot"}],
            "fallback_text": fallback_markdown,
            "raw_doctags": output
        }
    }

    # Populate content from DoclingDocument
    if hasattr(doc, 'main_text') and doc.main_text:
        for text_item in doc.main_text:
            item_text = text_item.text if hasattr(text_item, 'text') else str(text_item)
            if item_text not in seen_texts:  # Additional deduplication
                structured_data["document"]["content"].append({
                    "type": "text",
                    "value": item_text
                })
                seen_texts.add(item_text)
    if hasattr(doc, 'tables') and doc.tables:
        for table in doc.tables:
            structured_data["document"]["content"].append({
                "type": "table",
                "value": str(table)
            })

    print("Saving structured data to JSON...")
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(structured_data, f, indent=2)
    print(f"Structured data saved to {output_file}")

    # Save as Markdown file with fallback
    out_path = Path("./output.md")
    with open(out_path, 'w', encoding='utf-8') as f:
        markdown_content = doc.export_to_markdown() if doc.export_to_markdown() else fallback_markdown
        f.write(markdown_content)
    print(f"Document saved as Markdown to: {str(out_path.resolve())} with {len(markdown_content)} characters")

    return structured_data

# Main execution
def main():
    url = "https://aitrailblazer.com/"  # Your requested URL
    print(f"Fetching content from {url}...")
    
    fetch_result = webpage_to_image(url)
    if not fetch_result:
        return

    structured_data = process_with_docling_core(fetch_result, url)
    if structured_data:
        print("Example structured output:")
        print(json.dumps(structured_data, indent=2))

if __name__ == "__main__":
    main()