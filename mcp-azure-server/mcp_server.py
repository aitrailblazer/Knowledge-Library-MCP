import os
import asyncio
import json
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from mcp.server.fastmcp import FastMCP, Context
from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.projects.models import MessageRole
from dotenv import load_dotenv
from typing import List, Optional, Dict, Union, Callable
from functools import wraps
import yfinance as yf
import aiohttp
import webbrowser

load_dotenv()
app = FastAPI(title="MCP Full Server", description="MCP server compliant with 2024-11-05 spec")

tool_registry: Dict[str, Dict] = {}

def register_tool(name: str, description: str, input_schema: Dict):
    def decorator(func: Callable):
        tool_registry[name] = {
            "name": name,
            "description": description,
            "inputSchema": input_schema
        }
        @wraps(func)
        async def wrapper(*args, **kwargs):
            return await func(*args, **kwargs)
        return wrapper
    return decorator

mcp = FastMCP(
    name="FullMcpServer",
    description="A complete MCP server with Azure AI Agent and Microsoft Graph integration",
    dependencies=["azure-identity", "python-dotenv", "azure-ai-projects", "fastapi", "uvicorn", "yfinance", "aiohttp"],
    capabilities={"prompts": {"listChanged": True}, "tools": {"listChanged": True}}
)

credential = DefaultAzureCredential()
project_connection_string = os.getenv("PROJECT_CONNECTION_STRING")
agent_id = os.getenv("DEFAULT_AGENT_ID", "McpAgent")
ai_client = None
if project_connection_string:
    ai_client = AIProjectClient.from_connection_string(
        credential=credential,
        conn_str=project_connection_string,
        user_agent="mcp-full-server",
    )

# Microsoft Graph Configuration
CLIENT_ID = os.getenv("MS_GRAPH_CLIENT_ID")
TENANT_ID = os.getenv("MS_GRAPH_TENANT_ID", "common")
CLIENT_SECRET = os.getenv("MS_GRAPH_CLIENT_SECRET")
REDIRECT_URI = "http://localhost:8000/auth/callback"
SCOPES = ["Files.Read", "offline_access"]
TOKEN_FILE = "tokens.json"
access_token = None
refresh_token = None

# Load tokens from file on startup
def load_tokens():
    global access_token, refresh_token
    try:
        if os.path.exists(TOKEN_FILE):
            with open(TOKEN_FILE, "r") as f:
                tokens = json.load(f)
                access_token = tokens.get("access_token")
                refresh_token = tokens.get("refresh_token")
                print(f"Loaded tokens from {TOKEN_FILE}: access_token={access_token[:10]}..., refresh_token={refresh_token[:10]}...")
    except Exception as e:
        print(f"Error loading tokens: {str(e)}")

# Save tokens to file
def save_tokens():
    global access_token, refresh_token
    try:
        with open(TOKEN_FILE, "w") as f:
            json.dump({"access_token": access_token, "refresh_token": refresh_token}, f, indent=2)
        print(f"Saved tokens to {TOKEN_FILE}")
    except Exception as e:
        print(f"Error saving tokens: {str(e)}")

# Refresh access token using refresh token
async def refresh_access_token():
    global access_token, refresh_token
    if not refresh_token:
        print("No refresh token available. Authentication required.")
        return False
    
    token_url = f"https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token"
    payload = {
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET,
        "grant_type": "refresh_token",
        "refresh_token": refresh_token,
        "scope": " ".join(SCOPES)
    }
    print(f"Attempting to refresh access token with payload: {payload}")
    
    async with aiohttp.ClientSession() as session:
        async with session.post(token_url, data=payload, headers={"Content-Type": "application/x-www-form-urlencoded"}) as response:
            response_text = await response.text()
            print(f"Refresh token response status: {response.status}, body: {response_text}")
            if response.status == 200:
                token_data = json.loads(response_text)
                access_token = token_data["access_token"]
                refresh_token = token_data.get("refresh_token", refresh_token)
                save_tokens()
                print("Access token refreshed successfully.")
                return True
            else:
                print(f"Failed to refresh token: {response_text}")
                return False

# Load tokens on startup
load_tokens()

class ToolArgument(BaseModel):
    type: str
    description: str

class ToolInputSchema(BaseModel):
    type: str = "object"
    properties: Dict[str, ToolArgument]
    required: Optional[List[str]] = []

class ToolDefinition(BaseModel):
    name: str
    description: str
    inputSchema: ToolInputSchema

class ToolListResponse(BaseModel):
    tools: List[ToolDefinition]
    nextCursor: Optional[str] = None

class ToolContent(BaseModel):
    type: str
    text: Optional[str] = None
    data: Optional[Dict] = None

class ToolCallResponse(BaseModel):
    content: List[ToolContent]
    isError: bool = False

class ToolCallRequest(BaseModel):
    name: str
    arguments: Dict[str, Union[str, int]]

class Message(BaseModel):
    text: str

class AddNumbersRequest(BaseModel):
    a: int
    b: int

class StockPricesRequest(BaseModel):
    ticker: str
    interval: str = "1d"
    period: str = "1mo"

async def query_agent(agent_id: str, query: str) -> str:
    if not ai_client:
        return "Error: Azure AI Agent client not initialized."
    try:
        thread = await ai_client.agents.create_thread()
        thread_id = thread.id
        await ai_client.agents.create_message(
            thread_id=thread_id, role=MessageRole.USER, content=query
        )
        run = await ai_client.agents.create_run(thread_id=thread_id, agent_id=agent_id)
        while run.status in ["queued", "in_progress", "requires_action"]:
            await asyncio.sleep(1)
            run = await ai_client.agents.get_run(thread_id=thread_id, run_id=run.id)
        if run.status == "failed":
            return f"Error: Agent run failed - {run.last_error}"
        response_messages = await ai_client.agents.list_messages(thread_id=thread_id)
        response_message = response_messages.get_last_message_by_role(MessageRole.AGENT)
        return response_message.text_messages[0].text.value.strip() if response_message else "No response."
    except Exception as e:
        return f"Error querying agent: {str(e)}"

async def get_tool_list() -> str:
    tools = list(tool_registry.values())
    return json.dumps({"tools": tools, "nextCursor": None}, indent=2)

@mcp.prompt()
async def process_request(text: str) -> str:
    if not project_connection_string:
        return f"Processed locally: {text}"
    tool_list = await get_tool_list()
    instructions = (
        "You are an MCP server agent. Use the following tool list to process requests dynamically:\n"
        f"{tool_list}\n"
        "If the user asks for available tools (e.g., 'list tools'), return the tool list as text.\n"
        "For other requests, if a tool applies, return a JSON tool call: "
        '{"tool": "name", "arguments": {"key": "value"}}. Otherwise, respond naturally.'
    )
    query = f"{text}\n\n{instructions}"
    return await query_agent(agent_id, query)

@mcp.tool()
@register_tool(
    name="add_numbers",
    description="Add two integers together",
    input_schema={
        "type": "object",
        "properties": {
            "a": {"type": "integer", "description": "First number"},
            "b": {"type": "integer", "description": "Second number"}
        },
        "required": ["a", "b"]
    }
)
async def add_numbers(a: int, b: int, ctx: Context = None) -> str:
    try:
        result = a + b
        return f"Sum of {a} and {b} is {result}"
    except Exception as e:
        return f"Error computing sum: {str(e)}"

@mcp.tool()
@register_tool(
    name="get_stock_prices",
    description="Retrieve historical stock prices for a ticker from Yahoo Finance",
    input_schema={
        "type": "object",
        "properties": {
            "ticker": {"type": "string", "description": "Stock ticker symbol (e.g., AAPL)"},
            "interval": {"type": "string", "description": "Time interval (e.g., 1d, 1h, 1m), default 1d"},
            "period": {"type": "string", "description": "Time period (e.g., 1mo, 1y, 5d), default 1mo"}
        },
        "required": ["ticker"]
    }
)
async def get_stock_prices(ticker: str, interval: str = "1d", period: str = "1mo", ctx: Context = None) -> str:
    try:
        stock = yf.Ticker(ticker)
        hist = stock.history(period=period, interval=interval)
        if hist.empty:
            return json.dumps({"error": f"No data found for ticker {ticker} with interval {interval} and period {period}"}, indent=2)
        prices = [
            {"timestamp": date.strftime('%Y-%m-%d %H:%M:%S'), "close_price": round(price, 2)}
            for date, price in hist['Close'].tail(5).to_dict().items()
        ]
        result = {
            "ticker": ticker,
            "interval": interval,
            "period": period,
            "prices": prices
        }
        return json.dumps(result, indent=2)
    except Exception as e:
        return json.dumps({"error": f"Error fetching stock prices for {ticker}: {str(e)}"}, indent=2)

@mcp.tool()
@register_tool(
    name="list_onedrive_root",
    description="List files and folders in the user's OneDrive root directory",
    input_schema={
        "type": "object",
        "properties": {},
        "required": []
    }
)
async def list_onedrive_root(ctx: Context = None) -> str:
    global access_token, refresh_token
    if not access_token:
        if refresh_token:
            success = await refresh_access_token()
            if not success:
                return json.dumps({"error": "Token refresh failed. Please re-authenticate via /auth/start."}, indent=2)
        else:
            return json.dumps({"error": "Not authenticated. Please authenticate via /auth/start."}, indent=2)
    
    async with aiohttp.ClientSession() as session:
        headers = {"Authorization": f"Bearer {access_token}"}
        async with session.get("https://graph.microsoft.com/v1.0/me/drive/root/children", headers=headers) as response:
            if response.status == 200:
                data = await response.json()
                items = [{"name": item["name"], "id": item["id"], "type": "folder" if "folder" in item else "file"} for item in data["value"]]
                response_content = {"content": [{"type": "json", "text": None, "data": {"items": items}}], "isError": False}
                return json.dumps(response_content, indent=2)
            elif response.status == 401:
                success = await refresh_access_token()
                if success:
                    headers = {"Authorization": f"Bearer {access_token}"}
                    async with session.get("https://graph.microsoft.com/v1.0/me/drive/root/children", headers=headers) as resp:
                        if resp.status == 200:
                            data = await resp.json()
                            items = [{"name": item["name"], "id": item["id"], "type": "folder" if "folder" in item else "file"} for item in data["value"]]
                            response_content = {"content": [{"type": "json", "text": None, "data": {"items": items}}], "isError": False}
                            return json.dumps(response_content, indent=2)
                        else:
                            return json.dumps({"error": f"Failed to fetch OneDrive data after refresh: {resp.status}"}, indent=2)
                else:
                    return json.dumps({"error": "Token refresh failed. Please re-authenticate via /auth/start."}, indent=2)
            else:
                return json.dumps({"error": f"Failed to fetch OneDrive data: {response.status}"}, indent=2)

@mcp.resource("config://server")
async def get_config() -> str:
    return "Server configuration: version=1.0, env=prod"

@app.get("/auth/start")
async def auth_start():
    if not CLIENT_ID or not CLIENT_SECRET:
        print("Error: MS_GRAPH_CLIENT_ID or MS_GRAPH_CLIENT_SECRET not set in .env")
        raise HTTPException(status_code=500, detail="MS_GRAPH_CLIENT_ID or MS_GRAPH_CLIENT_SECRET not configured in .env")
    
    auth_url = (
        f"https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize?"
        f"client_id={CLIENT_ID}&response_type=code&redirect_uri={REDIRECT_URI}&"
        f"scope={'%20'.join(SCOPES)}"
    )
    print(f"Starting authentication flow...")
    print(f"Opening authorization URL: {auth_url}")
    webbrowser.open(auth_url)
    return {
        "message": "Authorization started. Please complete sign-in in your browser.",
        "auth_url": auth_url,
        "expected_redirect": f"{REDIRECT_URI}?code=<authorization_code>"
    }

@app.get("/auth/callback")
async def auth_callback(request: Request):
    global access_token, refresh_token
    query_params = dict(request.query_params)
    print(f"Callback received with query params: {query_params}")
    
    code = query_params.get("code")
    error = query_params.get("error")
    error_description = query_params.get("error_description")
    
    if error:
        print(f"Microsoft returned an error: {error} - {error_description}")
        raise HTTPException(status_code=400, detail=f"Authentication error: {error} - {error_description}")
    if not code:
        print("No authorization code received. Verify Entra ID config and redirect flow.")
        raise HTTPException(status_code=400, detail="No authorization code received. Start from /auth/start and check browser redirect.")
    
    token_url = f"https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token"
    payload = {
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET,
        "grant_type": "authorization_code",
        "code": code,
        "redirect_uri": REDIRECT_URI
    }
    print(f"Exchanging code for token with payload: {payload}")
    
    async with aiohttp.ClientSession() as session:
        async with session.post(token_url, data=payload, headers={"Content-Type": "application/x-www-form-urlencoded"}) as response:
            response_text = await response.text()
            print(f"Token response status: {response.status}, body: {response_text}")
            if response.status == 200:
                token_data = json.loads(response_text)
                access_token = token_data["access_token"]
                refresh_token = token_data.get("refresh_token")
                save_tokens()
                print("Authentication successful, tokens saved.")
                return {"message": "Authentication successful", "access_token": access_token}
            else:
                raise HTTPException(status_code=response.status, detail=f"Token exchange failed: {response_text}")

@app.get("/mcp/tools/list", response_model=ToolListResponse)
async def list_tools(cursor: Optional[str] = None):
    tools = [ToolDefinition(**tool) for tool in tool_registry.values()]
    return ToolListResponse(tools=tools, nextCursor=None)

@app.post("/mcp/tools/call")
async def call_tool(request: ToolCallRequest):
    name = request.name
    arguments = request.arguments
    if name not in tool_registry:
        raise HTTPException(status_code=400, detail={"code": -32602, "message": f"Unknown tool: {name}"})
    
    if name == "add_numbers":
        if "a" not in arguments or "b" not in arguments:
            raise HTTPException(status_code=400, detail={"code": -32602, "message": "Missing required arguments: a, b"})
        try:
            a = int(arguments["a"])
            b = int(arguments["b"])
            result = await add_numbers(a, b)
            response_content = {"content": [{"type": "text", "text": result, "data": None}], "isError": False}
            return JSONResponse(content=json.loads(json.dumps(response_content, indent=2)))
        except ValueError as e:
            response_content = {"content": [{"type": "text", "text": f"Invalid input: {str(e)}", "data": None}], "isError": True}
            return JSONResponse(content=json.loads(json.dumps(response_content, indent=2)))
        except Exception as e:
            response_content = {"content": [{"type": "text", "text": f"Tool execution error: {str(e)}", "data": None}], "isError": True}
            return JSONResponse(content=json.loads(json.dumps(response_content, indent=2)))
    
    if name == "get_stock_prices":
        if "ticker" not in arguments:
            raise HTTPException(status_code=400, detail={"code": -32602, "message": "Missing required argument: ticker"})
        try:
            ticker = str(arguments["ticker"])
            interval = str(arguments.get("interval", "1d"))
            period = str(arguments.get("period", "1mo"))
            result = await get_stock_prices(ticker, interval, period)
            response_content = {"content": [{"type": "json", "text": result, "data": None}], "isError": False}
            return JSONResponse(content=json.loads(json.dumps(response_content, indent=2)))
        except Exception as e:
            response_content = {"content": [{"type": "text", "text": f"Tool execution error: {str(e)}", "data": None}], "isError": True}
            return JSONResponse(content=json.loads(json.dumps(response_content, indent=2)))
    
    if name == "list_onedrive_root":
        result = await list_onedrive_root()
        return JSONResponse(content=json.loads(result))

@app.post("/mcp/process", response_model=dict)
async def process_message(message: Message):
    agent_response = await process_request(message.text)
    print(f"Agent response: {agent_response}")
    try:
        tool_call = json.loads(agent_response)
        if isinstance(tool_call, dict) and "tool" in tool_call and "arguments" in tool_call:
            if tool_call["tool"] == "add_numbers":
                a = tool_call["arguments"].get("a")
                b = tool_call["arguments"].get("b")
                if a is None or b is None:
                    return {"content": "Error: Missing arguments for add_numbers"}
                return {"content": await add_numbers(a, b)}
            elif tool_call["tool"] == "get_stock_prices":
                ticker = tool_call["arguments"].get("ticker")
                if ticker is None:
                    return {"content": "Error: Missing argument ticker for get_stock_prices"}
                interval = tool_call["arguments"].get("interval", "1d")
                period = tool_call["arguments"].get("period", "1mo")
                return {"content": await get_stock_prices(ticker, interval, period)}
            elif tool_call["tool"] == "list_onedrive_root":
                return {"content": await list_onedrive_root()}
            raise ValueError("Unknown tool")
        return {"content": agent_response}
    except json.JSONDecodeError:
        return {"content": agent_response}
    except ValueError as e:
        return {"content": f"Error: {str(e)}"}

@app.post("/mcp/add", response_model=dict)
async def add_numbers_endpoint(request: AddNumbersRequest):
    result = await add_numbers(request.a, request.b)
    return {"content": result}

@app.post("/mcp/stock_prices", response_model=dict)
async def stock_prices_endpoint(request: StockPricesRequest):
    result = await get_stock_prices(request.ticker, request.interval, request.period)
    return {"content": result}

@app.get("/mcp/config", response_model=dict)
async def get_config_endpoint():
    result = await get_config()
    return {"content": result}

@app.get("/health", response_model=dict)
async def health_check():
    return {
        "status": "running",
        "azure_connected": bool(project_connection_string),
        "ms_graph_authenticated": bool(access_token)
    }

if __name__ == "__main__":
    import uvicorn
    if not project_connection_string:
        print("Warning: PROJECT_CONNECTION_STRING not set, using local mock.")
    if not CLIENT_ID or not CLIENT_SECRET:
        print("Warning: MS_GRAPH_CLIENT_ID or MS_GRAPH_CLIENT_SECRET not set, Microsoft Graph integration disabled.")
    else:
        print(f"Starting server with existing tokens: access_token={access_token[:10] if access_token else None}..., refresh_token={refresh_token[:10] if refresh_token else None}...")
    uvicorn.run(app, host="0.0.0.0", port=8000)