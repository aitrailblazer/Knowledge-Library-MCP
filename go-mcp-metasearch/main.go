package main

import (
	"encoding/json"
	"fmt"
	"html"
	"io"
	"net/http"
	"net/url"
	"os"
)

// API Keys
var braveApiKey string

// Brave Search structs
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

type YahooChartResponse struct {
	Chart struct {
		Result []struct {
			Meta struct {
				RegularMarketPrice float64 `json:"regularMarketPrice"`
			} `json:"meta"`
			Timestamp  []int64 `json:"timestamp"`
			Indicators struct {
				Quote []struct {
					Close []float64 `json:"close"`
				} `json:"quote"`
			} `json:"indicators"`
		} `json:"result"`
	} `json:"chart"`
}

type StockHistoryEntry struct {
	Timestamp int64   `json:"timestamp"`
	Close     float64 `json:"close"`
}

// MCP structs
type MCPRequest struct {
	Tool       string                 `json:"tool"`
	Parameters map[string]interface{} `json:"parameters"`
}

type MCPResponse struct {
	ToolName     string              `json:"tool_name"`
	WebResults   []SearchResult      `json:"web_results"`
	NewsResults  []NewsResult        `json:"news_results"`
	ImageResults []interface{}       `json:"image_results"`
	VideoResults []interface{}       `json:"video_results"`
	StockPrice   *float64            `json:"stock_price,omitempty"`
	StockHistory []StockHistoryEntry `json:"stock_history,omitempty"`
}

// Capability Negotiation structs
type ToolCapability struct {
	Name        string           `json:"name"`
	Description string           `json:"description"`
	Parameters  map[string]Param `json:"parameters"`
}

type Param struct {
	Type     string `json:"type"`
	Required bool   `json:"required"`
}

type CapabilitiesResponse struct {
	Tools []ToolCapability `json:"tools"`
}

func main() {
	braveApiKey = os.Getenv("BRAVE_API_KEY")
	if braveApiKey == "" {
		fmt.Println("Error: BRAVE_API_KEY environment variable not set")
		os.Exit(1)
	}

	http.HandleFunc("/", handleMcpRequest)
	http.HandleFunc("/capabilities", handleCapabilitiesRequest)

	fmt.Println("Starting go-mcp-metasearch on http://localhost:8080...")
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

	fmt.Println("Handling tool:", request.Tool)

	handlers := map[string]func(map[string]interface{}) (MCPResponse, error){
		"company_stock_search": companyStockSearchHandler,
		"brave_news_search":    braveNewsSearchHandler,
		"yahoo_stock_history":  yahooStockHistoryHandler,
	}

	handler, exists := handlers[request.Tool]
	if !exists {
		http.Error(w, fmt.Sprintf("Tool %s not found", request.Tool), http.StatusNotFound)
		return
	}

	response, err := handler(request.Parameters)
	if err != nil {
		http.Error(w, fmt.Sprintf("Tool execution error: %v", err), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "  ")
	if err := encoder.Encode(response); err != nil {
		http.Error(w, "Failed to write response", http.StatusInternalServerError)
	}
}

func handleCapabilitiesRequest(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	capabilities := CapabilitiesResponse{
		Tools: []ToolCapability{
			{
				Name:        "company_stock_search",
				Description: "Fetches company overview from Brave Search and last stock price from Yahoo Finance",
				Parameters: map[string]Param{
					"ticker": {Type: "string", Required: true},
				},
			},
			{
				Name:        "brave_news_search",
				Description: "Fetches recent company news from Brave Search",
				Parameters: map[string]Param{
					"ticker": {Type: "string", Required: true},
				},
			},
			{
				Name:        "yahoo_stock_history",
				Description: "Fetches 5-day stock price history from Yahoo Finance",
				Parameters: map[string]Param{
					"ticker": {Type: "string", Required: true},
				},
			},
		},
	}

	w.Header().Set("Content-Type", "application/json")
	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "  ")
	if err := encoder.Encode(capabilities); err != nil {
		http.Error(w, "Failed to write capabilities response", http.StatusInternalServerError)
	}
}

func companyStockSearchHandler(params map[string]interface{}) (MCPResponse, error) {
	ticker, ok := params["ticker"].(string)
	if !ok {
		return MCPResponse{}, fmt.Errorf("ticker must be a string")
	}

	client := &http.Client{}
	response := MCPResponse{ToolName: "company_stock_search"}

	query := fmt.Sprintf("%s company overview", ticker)
	webResults, err := fetchBraveResults(client, "web", query)
	if err != nil {
		fmt.Println("Brave web search error:", err)
	} else {
		for i, result := range webResults {
			if i >= 3 {
				break
			}
			r := result.(SearchResult)
			r.Description = html.UnescapeString(r.Description)
			response.WebResults = append(response.WebResults, r)
		}
	}

	stockPrice, err := fetchYahooStockPrice(client, ticker)
	if err != nil {
		fmt.Println("Yahoo stock price error:", err)
	} else {
		response.StockPrice = &stockPrice
	}

	return response, nil
}

func braveNewsSearchHandler(params map[string]interface{}) (MCPResponse, error) {
	ticker, ok := params["ticker"].(string)
	if !ok {
		return MCPResponse{}, fmt.Errorf("ticker must be a string")
	}

	client := &http.Client{}
	response := MCPResponse{ToolName: "brave_news_search"}

	query := fmt.Sprintf("%s company news", ticker)
	newsResults, err := fetchBraveResults(client, "news", query)
	if err != nil {
		fmt.Println("Brave news search error:", err)
	} else {
		fmt.Println("Brave news results count:", len(newsResults))
		for i, result := range newsResults {
			if i >= 3 {
				break
			}
			r := result.(NewsResult)
			r.Description = html.UnescapeString(r.Description)
			response.NewsResults = append(response.NewsResults, r)
		}
	}

	return response, nil
}

func yahooStockHistoryHandler(params map[string]interface{}) (MCPResponse, error) {
	ticker, ok := params["ticker"].(string)
	if !ok {
		return MCPResponse{}, fmt.Errorf("ticker must be a string")
	}

	client := &http.Client{}
	response := MCPResponse{ToolName: "yahoo_stock_history"}

	historyResults, err := fetchYahooStockHistory(client, ticker)
	if err != nil {
		fmt.Println("Yahoo stock history error:", err)
	} else {
		response.StockHistory = historyResults
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
	default:
		return nil, fmt.Errorf("unsupported result type: %s", resultType)
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

	fmt.Println("Brave", resultType, "status code:", resp.StatusCode)

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	fmt.Println("Brave", resultType, "raw response length:", len(body))
	fmt.Println("Brave", resultType, "raw response:", string(body))

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
	}
	return nil, nil // Unreachable
}

func fetchYahooStockPrice(client *http.Client, ticker string) (float64, error) {
	apiURL := fmt.Sprintf("https://query1.finance.yahoo.com/v8/finance/chart/%s?interval=1d", ticker)
	req, err := http.NewRequest("GET", apiURL, nil)
	if err != nil {
		return 0, err
	}
	req.Header.Set("Accept", "application/json")
	req.Header.Set("User-Agent", "go-mcp-metasearch/1.0")

	resp, err := client.Do(req)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return 0, err
	}

	var chartResponse YahooChartResponse
	if err := json.Unmarshal(body, &chartResponse); err != nil {
		return 0, err
	}

	if len(chartResponse.Chart.Result) == 0 {
		return 0, fmt.Errorf("no stock data found for ticker %s", ticker)
	}

	return chartResponse.Chart.Result[0].Meta.RegularMarketPrice, nil
}

func fetchYahooStockHistory(client *http.Client, ticker string) ([]StockHistoryEntry, error) {
	apiURL := fmt.Sprintf("https://query1.finance.yahoo.com/v8/finance/chart/%s?interval=1d&range=5d", ticker)
	req, err := http.NewRequest("GET", apiURL, nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("Accept", "application/json")
	req.Header.Set("User-Agent", "go-mcp-metasearch/1.0")

	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	var chartResponse YahooChartResponse
	if err := json.Unmarshal(body, &chartResponse); err != nil {
		return nil, err
	}

	if len(chartResponse.Chart.Result) == 0 || len(chartResponse.Chart.Result[0].Timestamp) == 0 {
		return nil, fmt.Errorf("no stock history data found for ticker %s", ticker)
	}

	timestamps := chartResponse.Chart.Result[0].Timestamp
	closes := chartResponse.Chart.Result[0].Indicators.Quote[0].Close
	if len(timestamps) != len(closes) {
		return nil, fmt.Errorf("mismatched timestamp and close data for ticker %s", ticker)
	}

	history := make([]StockHistoryEntry, len(timestamps))
	for i := range timestamps {
		history[i] = StockHistoryEntry{
			Timestamp: timestamps[i],
			Close:     closes[i],
		}
	}
	return history, nil
}
