package tools

import (
	"encoding/json"
	"fmt"
	"html"
	"io"
	"net/http"
	"net/url"
	"os"
	"strings"
)

// Param defines a parameter’s type and requirement status
type Param struct {
	Type        string `json:"type"`
	Required    bool   `json:"required"`
	Description string `json:"description,omitempty"`
}

// Tool interface defines the contract for all MCP tools
type Tool interface {
	Name() string
	Description() string
	Parameters() map[string]Param
	Execute(params map[string]interface{}) (string, error)
	SubTools() map[string]Tool
}

// WebTools is a parent tool for web-related operations
type WebTools struct {
	subTools map[string]Tool
}

// NewWebTools creates a new instance of WebTools with sub-tools
func NewWebTools() Tool {
	wt := &WebTools{
		subTools: make(map[string]Tool),
	}
	wt.subTools["BraveSearch"] = &braveWebSearchTool{}
	return wt
}

func (wt *WebTools) Name() string {
	return "WebTools"
}

func (wt *WebTools) Description() string {
	return "A collection of web-related tools"
}

func (wt *WebTools) Parameters() map[string]Param {
	return map[string]Param{
		"operation": {Type: "string", Required: true}, // Dispatcher parameter
	}
}

func (wt *WebTools) SubTools() map[string]Tool {
	return wt.subTools
}

func (wt *WebTools) Execute(params map[string]interface{}) (string, error) {
	op, ok := params["operation"].(string)
	if !ok {
		return "", fmt.Errorf("operation must be a string")
	}

	tool, exists := wt.subTools[strings.Title(op)]
	if !exists {
		return "", fmt.Errorf("operation '%s' not found in WebTools", op)
	}

	return tool.Execute(params)
}

// braveWebSearchTool implements the Tool interface for Brave Search
type braveWebSearchTool struct{}

func (t *braveWebSearchTool) Name() string {
	return "BraveSearch"
}

func (t *braveWebSearchTool) Description() string {
	return "Search the web using the Brave Search API and return results in Markdown format"
}

// Add a new parameter for search type and modify the API URL based on it
func (t *braveWebSearchTool) Parameters() map[string]Param {
	return map[string]Param{
		"query":       {Type: "string", Required: true},
		"api_key":     {Type: "string", Required: false},
		"search_type": {Type: "string", Required: false, Description: "Type of search: web, image, video, news"},
		"safesearch":  {Type: "string", Required: false, Description: "Safe search level: strict, moderate, or off"},
		"count":       {Type: "int", Required: false, Description: "Number of results to return"},
		"search_lang": {Type: "string", Required: false, Description: "Language for the search results"},
		"country":     {Type: "string", Required: false, Description: "Country for the search results"},
		"spellcheck":  {Type: "bool", Required: false, Description: "Enable or disable spellcheck"},
	}
}

func (t *braveWebSearchTool) SubTools() map[string]Tool {
	return nil // No sub-tools for BraveSearch
}

func (t *braveWebSearchTool) Execute(params map[string]interface{}) (string, error) {
	query, ok := params["query"].(string)
	if !ok {
		return "", fmt.Errorf("query must be a string")
	}
	apiKey, _ := params["api_key"].(string)
	if apiKey == "" {
		apiKey = os.Getenv("BRAVE_API_KEY")
	}
	searchType, _ := params["search_type"].(string)
	if searchType == "" {
		searchType = "web" // Default to web search
	}
	safesearch, _ := params["safesearch"].(string)
	count, _ := params["count"].(int)
	searchLang, _ := params["search_lang"].(string)
	country, _ := params["country"].(string)
	spellcheck, _ := params["spellcheck"].(bool)

	client := &http.Client{}
	apiURL := fmt.Sprintf("https://api.search.brave.com/res/v1/%s/search?q=%s", searchType, url.QueryEscape(query))

	// Add optional parameters to the URL
	queryParams := url.Values{}
	if safesearch != "" {
		queryParams.Add("safesearch", safesearch)
	}
	if count > 0 {
		queryParams.Add("count", fmt.Sprintf("%d", count))
	}
	if searchLang != "" {
		queryParams.Add("search_lang", searchLang)
	}
	if country != "" {
		queryParams.Add("country", country)
	}
	if spellcheck {
		queryParams.Add("spellcheck", "1")
	} else {
		queryParams.Add("spellcheck", "0")
	}

	if len(queryParams) > 0 {
		apiURL += "&" + queryParams.Encode()
	}

	req, err := http.NewRequest("GET", apiURL, nil)
	if err != nil {
		return "", fmt.Errorf("failed to create request: %v", err)
	}
	req.Header.Set("Accept", "application/json")
	req.Header.Set("X-Subscription-Token", apiKey)

	resp, err := client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to execute request: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return "", fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return "", fmt.Errorf("failed to read response body: %v", err)
	}

	// Log the raw response body for debugging
	fmt.Printf("Raw API Response: %s\n", string(body))

	// Handle different response structures based on search type
	if searchType == "news" {
		type NewsSearchResponse struct {
			Results []struct {
				Title       string `json:"title"`
				Description string `json:"description"`
				Url         string `json:"url"`
			} `json:"results"`
		}
		var newsResponse NewsSearchResponse
		if err := json.Unmarshal(body, &newsResponse); err != nil {
			return "", fmt.Errorf("failed to parse response: %v", err)
		}

		markdown := "## Brave News Search Results"
		for i, result := range newsResponse.Results {
			if i >= 3 {
				break
			}
			if i > 0 {
				markdown += "\n"
			}
			markdown += fmt.Sprintf("\n- **%s**\n  %s\n  %s", result.Title, result.Url, result.Description)
		}
		if len(newsResponse.Results) == 0 {
			markdown += "\nNo results found."
		}
		return markdown, nil
	}

	// Default to web search response handling
	type WebSearchResponse struct {
		Web struct {
			Results []struct {
				Title       string `json:"title"`
				Description string `json:"description"`
				Url         string `json:"url"`
			} `json:"results"`
		} `json:"web"`
	}
	var webResponse WebSearchResponse
	if err := json.Unmarshal(body, &webResponse); err != nil {
		return "", fmt.Errorf("failed to parse response: %v", err)
	}

	markdown := "## Brave Web Search Results"
	for i, result := range webResponse.Web.Results {
		if i >= 3 {
			break
		}
		result.Description = html.UnescapeString(result.Description)
		if i > 0 {
			markdown += "\n"
		}
		markdown += fmt.Sprintf("\n- **%s**\n  %s\n  %s", result.Title, result.Url, result.Description)
	}
	if len(webResponse.Web.Results) == 0 {
		markdown += "\nNo results found."
	}

	return markdown, nil
}
