package main

import (
	"encoding/json"
	"fmt"
	"html" // Added for HTML unescaping
	"io"
	"net/http"
	"net/url"
	"os"
)

var braveApiKey string

type BraveSearchResponse struct {
	Web struct {
		Results []SearchResult `json:"results"`
	} `json:"web"`
}

type SearchResult struct {
	Title       string `json:"title"`
	Description string `json:"description"`
	Url         string `json:"url"`
}

type BraveNewsResponse struct {
	News struct {
		Results []NewsResult `json:"results"`
	} `json:"news"`
}

type NewsResult struct {
	Title       string `json:"title"`
	Description string `json:"description"`
	Url         string `json:"url"`
	Published   string `json:"published"`
}

type BraveImageResponse struct {
	Images struct {
		Results []ImageResult `json:"results"`
	} `json:"images"`
}

type ImageResult struct {
	Title  string `json:"title"`
	Url    string `json:"url"`
	Source string `json:"source"`
}

type BraveVideoResponse struct {
	Videos struct {
		Results []VideoResult `json:"results"`
	} `json:"videos"`
}

type VideoResult struct {
	Title     string `json:"title"`
	Url       string `json:"url"`
	Duration  string `json:"duration"`
	Thumbnail string `json:"thumbnail"`
}

type MCPRequest struct {
	Tool       string                 `json:"tool"`
	Parameters map[string]interface{} `json:"parameters"`
}

type MCPResponse struct {
	ToolName     string         `json:"tool_name"`
	WebResults   []SearchResult `json:"web_results"`
	NewsResults  []NewsResult   `json:"news_results"`
	ImageResults []ImageResult  `json:"image_results"`
	VideoResults []VideoResult  `json:"video_results"`
}

func main() {
	braveApiKey = os.Getenv("BRAVE_API_KEY")
	if braveApiKey == "" {
		fmt.Println("Error: BRAVE_API_KEY environment variable not set")
		os.Exit(1)
	}

	http.HandleFunc("/", handleMcpRequest)

	fmt.Println("Starting go-mcp-brave custom MCP server on http://localhost:8080...")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		fmt.Printf("Server error: %v\n", err)
	}
}

func handleMcpRequest(w http.ResponseWriter, r *http.Request) {
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

	if request.Tool != "brave_web_search" {
		http.Error(w, fmt.Sprintf("Tool %s not found", request.Tool), http.StatusNotFound)
		return
	}

	response, err := braveSearchHandler(request.Parameters)
	if err != nil {
		http.Error(w, fmt.Sprintf("Tool execution error: %v", err), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "  ") // Enable pretty printing
	if err := encoder.Encode(response); err != nil {
		http.Error(w, "Failed to write response", http.StatusInternalServerError)
	}
}

func braveSearchHandler(params map[string]interface{}) (MCPResponse, error) {
	query, ok := params["query"].(string)
	if !ok {
		return MCPResponse{}, fmt.Errorf("query must be a string")
	}

	client := &http.Client{}
	response := MCPResponse{ToolName: "brave_web_search"}

	// Web Search
	webResults, err := fetchBraveResults(client, "web", query)
	if err != nil {
		fmt.Println("Web search error:", err)
	} else {
		for i, result := range webResults {
			if i >= 3 {
				break
			}
			r := result.(SearchResult)
			r.Description = html.UnescapeString(r.Description) // Decode HTML entities
			response.WebResults = append(response.WebResults, r)
		}
	}

	// News Search
	newsResults, err := fetchBraveResults(client, "news", query)
	if err != nil {
		fmt.Println("News search error:", err)
	} else {
		for i, result := range newsResults {
			if i >= 3 {
				break
			}
			r := result.(NewsResult)
			r.Description = html.UnescapeString(r.Description) // Decode HTML entities
			response.NewsResults = append(response.NewsResults, r)
		}
	}

	// Image Search
	imageResults, err := fetchBraveResults(client, "images", query)
	if err != nil {
		fmt.Println("Image search error:", err)
	} else {
		for i, result := range imageResults {
			if i >= 3 {
				break
			}
			r := result.(ImageResult)
			r.Title = html.UnescapeString(r.Title) // Decode HTML entities (if any)
			response.ImageResults = append(response.ImageResults, r)
		}
	}

	// Video Search
	videoResults, err := fetchBraveResults(client, "videos", query)
	if err != nil {
		fmt.Println("Video search error:", err)
	} else {
		for i, result := range videoResults {
			if i >= 3 {
				break
			}
			r := result.(VideoResult)
			r.Title = html.UnescapeString(r.Title) // Decode HTML entities (if any)
			response.VideoResults = append(response.VideoResults, r)
		}
	}

	return response, nil
}

func fetchBraveResults(client *http.Client, resultType, query string) ([]interface{}, error) {
	var endpoint string
	switch resultType {
	case "web":
		endpoint = "https://api.search.brave.com/res/v1/web/search"
	case "news":
		endpoint = "https://api.search.brave.com/res/v1/news/search"
	case "images":
		endpoint = "https://api.search.brave.com/res/v1/images/search"
	case "videos":
		endpoint = "https://api.search.brave.com/res/v1/videos/search"
	default:
		return nil, fmt.Errorf("unknown result type: %s", resultType)
	}

	apiURL := fmt.Sprintf("%s?q=%s", endpoint, url.QueryEscape(query))
	req, err := http.NewRequest("GET", apiURL, nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("Accept", "application/json")
	req.Header.Set("X-Subscription-Token", braveApiKey)

	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	switch resultType {
	case "web":
		var searchResponse BraveSearchResponse
		if err := json.Unmarshal(body, &searchResponse); err != nil {
			return nil, err
		}
		results := make([]interface{}, len(searchResponse.Web.Results))
		for i, r := range searchResponse.Web.Results {
			results[i] = r
		}
		return results, nil
	case "news":
		var newsResponse BraveNewsResponse
		if err := json.Unmarshal(body, &newsResponse); err != nil {
			return nil, err
		}
		results := make([]interface{}, len(newsResponse.News.Results))
		for i, r := range newsResponse.News.Results {
			results[i] = r
		}
		return results, nil
	case "images":
		var imageResponse BraveImageResponse
		if err := json.Unmarshal(body, &imageResponse); err != nil {
			return nil, err
		}
		results := make([]interface{}, len(imageResponse.Images.Results))
		for i, r := range imageResponse.Images.Results {
			results[i] = r
		}
		return results, nil
	case "videos":
		var videoResponse BraveVideoResponse
		if err := json.Unmarshal(body, &videoResponse); err != nil {
			return nil, err
		}
		results := make([]interface{}, len(videoResponse.Videos.Results))
		for i, r := range videoResponse.Videos.Results {
			results[i] = r
		}
		return results, nil
	}
	return nil, nil // Unreachable, but satisfies return type
}
