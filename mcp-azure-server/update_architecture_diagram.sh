#!/bin/bash

# Set the base directory to the directory of the script
BASEDIR=$(dirname "$0")
cd "$BASEDIR"

# Check for required dependencies
if ! command -v mmdc &> /dev/null; then
    echo "mermaid-cli not found. Installing..."
    npm install -g @mermaid-js/mermaid-cli
fi

# Create img directory if it doesn't exist
mkdir -p img

# Generate the architecture diagram
echo "Generating architecture diagram..."
mmdc -i architecture.mmd -o "img/mcp-azure-server-Architecture.png" -b white -w 1920 -H 1080
# Also create a copy in the root directory for backward compatibility
cp "img/mcp-azure-server-Architecture.png" "architecture.png"

echo "Architecture diagram generated successfully: img/mcp-azure-server-Architecture.png"
echo "Also copied to: architecture.png for backward compatibility"