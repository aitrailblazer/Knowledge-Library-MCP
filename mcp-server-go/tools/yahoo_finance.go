package tools

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"time"
)

// FinanceTools is a parent tool for finance-related operations
type FinanceTools struct {
	subTools map[string]Tool
}

// NewFinanceTools creates a new instance of FinanceTools with sub-tools
func NewFinanceTools() Tool {
	ft := &FinanceTools{
		subTools: make(map[string]Tool),
	}
	ft.subTools["YahooStockPrice"] = &yahooStockPriceTool{}
	return ft
}

func (ft *FinanceTools) Name() string {
	return "FinanceTools"
}

func (ft *FinanceTools) Description() string {
	return "A collection of financial tools"
}

func (ft *FinanceTools) Parameters() map[string]Param {
	return map[string]Param{
		"operation": {Type: "string", Required: true}, // Dispatcher parameter
	}
}

func (ft *FinanceTools) SubTools() map[string]Tool {
	return ft.subTools
}

func (ft *FinanceTools) Execute(params map[string]interface{}) (string, error) {
	op, ok := params["operation"].(string)
	if !ok {
		return "", fmt.Errorf("operation must be a string")
	}

	tool, exists := ft.subTools[op]
	if !exists {
		return "", fmt.Errorf("operation '%s' not found in FinanceTools", op)
	}

	return tool.Execute(params)
}

// StockPriceEntry represents a single day's stock price data
type StockPriceEntry struct {
	Date   string  `json:"date"`
	Open   float64 `json:"open"`
	High   float64 `json:"high"`
	Low    float64 `json:"low"`
	Close  float64 `json:"close"`
	Volume int64   `json:"volume"`
}

// StockPriceResponse represents the structured output
type StockPriceResponse struct {
	Ticker   string            `json:"ticker"`
	Interval string            `json:"interval"`
	Period   string            `json:"period"`
	Prices   []StockPriceEntry `json:"prices"`
}

// yahooStockPriceTool implements the Tool interface for Yahoo Finance
type yahooStockPriceTool struct{}

func (t *yahooStockPriceTool) Name() string {
	return "YahooStockPrice"
}

func (t *yahooStockPriceTool) Description() string {
	return "Fetch historical stock prices from Yahoo Finance with a specified interval in structured JSON format"
}

func (t *yahooStockPriceTool) Parameters() map[string]Param {
	return map[string]Param{
		"ticker":   {Type: "string", Required: true},
		"interval": {Type: "string", Required: true},  // e.g., "1d", "1wk", "1mo"
		"period":   {Type: "string", Required: false}, // e.g., "1mo", "3mo", "1y"
	}
}

func (t *yahooStockPriceTool) SubTools() map[string]Tool {
	return nil // No sub-tools for YahooStockPrice
}

func (t *yahooStockPriceTool) Execute(params map[string]interface{}) (string, error) {
	ticker, ok := params["ticker"].(string)
	if !ok {
		return "", fmt.Errorf("ticker must be a string")
	}
	interval, ok := params["interval"].(string)
	if !ok {
		return "", fmt.Errorf("interval must be a string")
	}
	period, _ := params["period"].(string)
	if period == "" {
		period = "1mo" // Default to 1 month if not specified
	}

	// Construct Yahoo Finance API URL
	apiURL := fmt.Sprintf("https://query1.finance.yahoo.com/v8/finance/chart/%s?interval=%s&range=%s&includePrePost=false",
		url.QueryEscape(ticker), url.QueryEscape(interval), url.QueryEscape(period))

	client := &http.Client{}
	req, err := http.NewRequest("GET", apiURL, nil)
	if err != nil {
		return "", fmt.Errorf("failed to create request: %v", err)
	}
	req.Header.Set("Accept", "application/json")
	req.Header.Set("User-Agent", "mcp-server-go/1.0")

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

	// Parse Yahoo Finance response
	type YahooChartResponse struct {
		Chart struct {
			Result []struct {
				Meta struct {
					Currency           string  `json:"currency"`
					Symbol             string  `json:"symbol"`
					RegularMarketPrice float64 `json:"regularMarketPrice"`
				} `json:"meta"`
				Timestamp  []int64 `json:"timestamp"`
				Indicators struct {
					Quote []struct {
						Open   []float64 `json:"open"`
						High   []float64 `json:"high"`
						Low    []float64 `json:"low"`
						Close  []float64 `json:"close"`
						Volume []int64   `json:"volume"`
					} `json:"quote"`
				} `json:"indicators"`
			} `json:"result"`
			Error interface{} `json:"error"`
		} `json:"chart"`
	}

	var chartResponse YahooChartResponse
	if err := json.Unmarshal(body, &chartResponse); err != nil {
		return "", fmt.Errorf("failed to parse response: %v", err)
	}

	if len(chartResponse.Chart.Result) == 0 || chartResponse.Chart.Error != nil {
		return "", fmt.Errorf("no stock data found for ticker %s", ticker)
	}

	result := chartResponse.Chart.Result[0]
	if len(result.Timestamp) == 0 || len(result.Indicators.Quote) == 0 {
		return "", fmt.Errorf("no historical data available for ticker %s", ticker)
	}

	// Build structured response
	stockResponse := StockPriceResponse{
		Ticker:   ticker,
		Interval: interval,
		Period:   period,
		Prices:   make([]StockPriceEntry, 0, len(result.Timestamp)),
	}

	for i := 0; i < len(result.Timestamp) && i < 5; i++ { // Limit to 5 entries for brevity
		t := time.Unix(result.Timestamp[i], 0).Format("2006-01-02")
		quote := result.Indicators.Quote[0]
		entry := StockPriceEntry{
			Date:   t,
			Open:   quote.Open[i],
			High:   quote.High[i],
			Low:    quote.Low[i],
			Close:  quote.Close[i],
			Volume: quote.Volume[i],
		}
		stockResponse.Prices = append(stockResponse.Prices, entry)
	}

	// Convert structured response to JSON string
	jsonData, err := json.Marshal(stockResponse)
	if err != nil {
		return "", fmt.Errorf("failed to marshal structured response: %v", err)
	}

	return string(jsonData), nil
}
