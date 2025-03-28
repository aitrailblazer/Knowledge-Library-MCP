#!/bin/bash
# update_architecture_diagram.sh
# Script to render the architecture diagram when architecture.mmd changes
# Usage: ./update_architecture_diagram.sh
# Set the base directory
BASE_DIR="/Users/constantinevassilev02/MyLocalDocuments/go-projects/Knowledge-Library-MCP/KnowledgeLibraryMCP"
MMD_FILE="$BASE_DIR/architecture.mmd"
OUTPUT_DIR="$BASE_DIR/img"
OUTPUT_FILE="$OUTPUT_DIR/KnowledgeLibraryMCP-Architecture.png"
# Check if the Mermaid CLI is installed
if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi
# Make sure the output directory exists
mkdir -p "$OUTPUT_DIR"
echo "Generating architecture diagram..."
echo "Source: $MMD_FILE"
echo "Output: $OUTPUT_FILE"
# Generate the diagram with high resolution and white background
npx @mermaid-js/mermaid-cli -i "$MMD_FILE" -o "$OUTPUT_FILE" -b white -w 1920 -H 1080
# Check if the diagram was generated successfully
if [ $? -eq 0 ]; then
    echo "Architecture diagram generated successfully."
    echo "Location: $OUTPUT_FILE"
    
    # Get the file size in human-readable format
    FILE_SIZE=$(du -h "$OUTPUT_FILE" | cut -f1)
    echo "File size: $FILE_SIZE"
    
    # Update the README.md if architecture diagram section exists
    if grep -q "Architecture Diagram" "$BASE_DIR/README.md"; then
        echo "README.md already contains Architecture Diagram section."
    else
        echo "Consider adding the diagram to your README.md."
    fi
else
    echo "Error: Failed to generate architecture diagram."
    exit 1
fi
echo "Done."