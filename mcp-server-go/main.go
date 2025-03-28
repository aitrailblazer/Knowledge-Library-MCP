package main

import (
	"encoding/json"
	"fmt"
	"io"
	"mcp-server-go/tools" // Import the tools package
	"net/http"
	"os"
	"strings"
)

// MCPRequest defines the structure of an incoming MCP request
type MCPRequest struct {
	Tool       string                 `json:"tool"`
	Parameters map[string]interface{} `json:"parameters"`
}

// MCPResponse defines the structure of the MCP server’s response
type MCPResponse struct {
	Result string `json:"result"` // Markdown-formatted result
}

// ToolCapability describes a tool’s metadata
type ToolCapability struct {
	Name        string                 `json:"name"`
	Description string                 `json:"description"`
	Parameters  map[string]tools.Param `json:"parameters"`
	SubTools    []ToolCapability       `json:"subtools,omitempty"` // Hierarchical support
	Category    string                 `json:"category,omitempty"` // For discovery
	Tags        []string               `json:"tags,omitempty"`     // For filtering
}

// CapabilitiesResponse lists all available tools
type CapabilitiesResponse struct {
	Tools []ToolCapability `json:"tools"`
}

// toolRegistry holds all registered tools
var toolRegistry = make(map[string]tools.Tool)

// RegisterTool adds a tool to the registry
func RegisterTool(t tools.Tool) {
	toolRegistry[t.Name()] = t
}

func main() {
	// Load environment variables
	if os.Getenv("BRAVE_API_KEY") == "" {
		fmt.Println("Error: BRAVE_API_KEY environment variable not set")
		os.Exit(1)
	}

	// Register tools from the tools package
	RegisterTool(tools.NewWebTools())
	RegisterTool(tools.NewFinanceTools())

	// Register HTTP handlers
	http.HandleFunc("/tools", handleToolsRequest)
	http.HandleFunc("/invoke", handleInvokeRequest)

	// Start the server
	fmt.Println("Starting MCP server on http://localhost:8080...")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		fmt.Printf("Server failed to start: %v\n", err)
	}
}

// handleToolsRequest lists all registered tools with hierarchical structure
func handleToolsRequest(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	capabilities := CapabilitiesResponse{
		Tools: make([]ToolCapability, 0, len(toolRegistry)),
	}
	for _, tool := range toolRegistry {
		toolCap := buildToolCapability(tool)
		capabilities.Tools = append(capabilities.Tools, toolCap)
	}

	w.Header().Set("Content-Type", "application/json")
	jsonData, err := json.MarshalIndent(capabilities, "", "  ")
	if err != nil {
		http.Error(w, "Failed to encode capabilities", http.StatusInternalServerError)
		return
	}
	w.Write(jsonData)
}

// buildToolCapability constructs a ToolCapability recursively
func buildToolCapability(tool tools.Tool) ToolCapability {
	toolParams := tool.Parameters()
	params := make(map[string]tools.Param, len(toolParams))
	for k, v := range toolParams {
		params[k] = v
	}

	cap := ToolCapability{
		Name:        tool.Name(),
		Description: tool.Description(),
		Parameters:  params,
	}

	// Add sub-tools if present
	subTools := tool.SubTools()
	if len(subTools) > 0 {
		cap.SubTools = make([]ToolCapability, 0, len(subTools))
		for _, subTool := range subTools {
			cap.SubTools = append(cap.SubTools, buildToolCapability(subTool))
		}
	}

	return cap
}

// handleInvokeRequest executes the requested tool
func handleInvokeRequest(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	body, err := io.ReadAll(r.Body)
	if err != nil {
		http.Error(w, "Failed to read request body", http.StatusBadRequest)
		return
	}
	defer r.Body.Close()

	var request MCPRequest
	if err := json.Unmarshal(body, &request); err != nil {
		http.Error(w, "Invalid MCP request format", http.StatusBadRequest)
		return
	}

	// Split tool path for hierarchical invocation (e.g., "WebTools.BraveSearch")
	toolParts := splitToolPath(request.Tool)
	tool, exists := toolRegistry[toolParts[0]]
	if !exists {
		http.Error(w, fmt.Sprintf("Tool '%s' not found", request.Tool), http.StatusNotFound)
		return
	}

	// Navigate sub-tools if specified
	if len(toolParts) > 1 {
		for _, part := range toolParts[1:] {
			subTools := tool.SubTools()
			tool, exists = subTools[part]
			if !exists {
				http.Error(w, fmt.Sprintf("Sub-tool '%s' not found", request.Tool), http.StatusNotFound)
				return
			}
		}
	}

	result, err := tool.Execute(request.Parameters)
	if err != nil {
		http.Error(w, fmt.Sprintf("Tool execution failed: %v", err), http.StatusInternalServerError)
		return
	}

	response := MCPResponse{Result: result}
	w.Header().Set("Content-Type", "application/json")
	jsonData, err := json.MarshalIndent(response, "", "  ")
	if err != nil {
		http.Error(w, "Failed to encode response", http.StatusInternalServerError)
		return
	}
	w.Write(jsonData)
}

// splitToolPath splits a tool path into parts (e.g., "WebTools.BraveSearch" -> ["WebTools", "BraveSearch"])
func splitToolPath(path string) []string {
	return strings.Split(path, ".")
}
