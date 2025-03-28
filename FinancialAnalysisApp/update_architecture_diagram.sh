#!/bin/bash

# This script updates the architecture diagram for the FinancialAnalysisApp project.
# It uses the `mmdc` tool (Mermaid CLI) to generate a PNG from the architecture.mmd file.

# Ensure the script is run from the correct directory
SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &> /dev/null && pwd)
cd "$SCRIPT_DIR"

# Check if Mermaid CLI is installed
if ! command -v mmdc &> /dev/null
then
    echo "Mermaid CLI (mmdc) is not installed. Please install it first."
    echo "You can install it using npm: npm install -g @mermaid-js/mermaid-cli"
    exit 1
fi

# Define input and output files
INPUT_FILE="architecture.mmd"
OUTPUT_FILE="FinancialAnalysisApp-Architecture.png"
mkdir -p img

# Generate the diagram with white background
mmdc -i "$INPUT_FILE" -o "img/$OUTPUT_FILE" -b white -w 1920 -H 1080
# Also create a copy in the root directory for backward compatibility
cp "img/$OUTPUT_FILE" "architecture.png"

# Check if the output file was created successfully
if [ -f "img/$OUTPUT_FILE" ]; then
    echo "Architecture diagram updated successfully: img/$OUTPUT_FILE"
    echo "Also copied to: architecture.png for backward compatibility"
else
    echo "Failed to update the architecture diagram."
    exit 1
fi