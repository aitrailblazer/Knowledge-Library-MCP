#!/bin/bash
# Script to generate the architecture diagram for go-mcp-metasearch
# Ensure the script exits on any error
set -e
# Define the input and output files
INPUT_FILE="architecture.mmd"
OUTPUT_FILE="go-mcp-metasearch-Architecture.png"
mkdir -p img
# Check if mermaid-cli is installed
if ! command -v mmdc &> /dev/null
then
    echo "Error: mermaid-cli (mmdc) is not installed. Please install it to proceed."
    exit 1
fi
# Generate the diagram with white background
mmdc -i "$INPUT_FILE" -o "img/$OUTPUT_FILE" -b white -w 1920 -H 1080
# Also create a copy in the root directory for backward compatibility
cp "img/$OUTPUT_FILE" "architecture.png"
# Confirm success
echo "Architecture diagram generated successfully: img/$OUTPUT_FILE"
echo "Also copied to: architecture.png for backward compatibility"