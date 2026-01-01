"""
Unity Full Diagnostics
----------------------
Standalone diagnostic tool for Unity.
1. Checks for Console Errors.
2. If errors exist: Reports them and exits.
3. If no errors: Saves the current scene and runs all tests.
4. Produces a unified diagnostic report.

Usage:
  python tools/unity_full_diagnostics.py
"""

import asyncio
import httpx
import json
import sys
import argparse
import ast
from typing import Optional, Dict, Any, List

# --- Constants & Styling ---
MCP_URL = "http://localhost:8080/mcp"

class Style:
    RED = "\033[91m"
    YELLOW = "\033[93m"
    CYAN = "\033[96m"
    GREEN = "\033[92m"
    BOLD = "\033[1m"
    DIM = "\033[2m"
    UNDERLINE = "\033[4m"
    RESET = "\033[0m"

# --- Logic: MCP Client ---
class MCPClient:
    def __init__(self, base_url: str):
        self.base_url = base_url
        self.sid: Optional[str] = None
        self.client: Optional[httpx.AsyncClient] = None

    async def __aenter__(self):
        self.client = httpx.AsyncClient(timeout=300.0)
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.client:
            await self.client.aclose()

    async def connect(self):
        resp = await self.client.get(self.base_url, headers={"Accept": "application/json"})
        self.sid = resp.headers.get("mcp-session-id") or resp.json().get("sessionId")
        if not self.sid:
            raise RuntimeError("Failed to obtain MCP Session ID")

        await self.rpc("initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "unity-diagnostics", "version": "1.0"}
        }, msg_id=1)
        await self.rpc("notifications/initialized")

    async def rpc(self, method: str, params: Optional[Dict] = None, msg_id: Optional[int] = None):
        payload = {"jsonrpc": "2.0", "method": method}
        if params is not None: payload["params"] = params
        if msg_id is not None: payload["id"] = msg_id
        
        url = f"{self.base_url}?sessionId={self.sid}"
        headers = {"mcp-session-id": self.sid, "Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        
        r = await self.client.post(url, json=payload, headers=headers)
        r.raise_for_status()
        
        if msg_id is None: return None
        
        responses = []
        for line in r.text.splitlines():
            if line.startswith("data: "):
                try:
                    data = json.loads(line[6:])
                    responses.append(data)
                except: continue
        
        for res in responses:
            if res.get("id") == msg_id:
                return res
        for res in responses:
            if res.get("method") == "notifications/message":
                return res
        try:
            return r.json()
        except: return None

    async def call_tool(self, name: str, args: Optional[Dict[str, Any]] = None):
        return await self.rpc("tools/call", {"name": name, "arguments": args or {}}, msg_id=2)

# --- Logic: Formatters ---
class DiagnosticsFormatter:
    TYPE_COLORS = {
        "ERROR": Style.RED, "WARNING": Style.YELLOW, "LOG": Style.CYAN,
        "ASSERT": Style.RED, "EXCEPTION": Style.RED
    }

    @staticmethod
    def format_log(entry: Dict[str, Any]) -> str:
        etype = entry.get("type", "Log").upper()
        msg = entry.get("message", "")
        file = entry.get("file", "")
        line = entry.get("line", 0)
        color = DiagnosticsFormatter.TYPE_COLORS.get(etype, Style.RESET)
        header = f"{color}{Style.BOLD}[{etype}]{Style.RESET}"
        loc = f" {Style.UNDERLINE}{file}:{line}{Style.RESET}" if file else ""
        return f"{header}{loc}\n{msg}"

    @staticmethod
    def format_test(test: Dict[str, Any]) -> str:
        name = test.get("fullName") or test.get("name", "Unknown Test")
        state = test.get("state", "Inconclusive")
        duration = test.get("durationSeconds", 0.0)
        message = test.get("message")
        stack = test.get("stackTrace")
        
        color = Style.GREEN if state == "Passed" else Style.RED if state == "Failed" else Style.YELLOW
        output = f"{color}{Style.BOLD}[{state.upper()}]{Style.RESET} {name} ({duration:.3f}s)"
        if message: output += f"\n  {Style.RED}{message}{Style.RESET}"
        if stack:
            dim_stack = "\n".join([f"    {Style.DIM}{line}{Style.RESET}" for line in str(stack).splitlines()])
            output += f"\n{dim_stack}"
        return output

    @staticmethod
    def parse_complex_data(input_data: Any) -> Any:
        if not isinstance(input_data, str): return input_data
        try: return json.loads(input_data)
        except:
            if input_data.strip().startswith("{"):
                try: return ast.literal_eval(input_data)
                except: pass
        return input_data

# --- Execution Logic ---
async def run_diagnostics():
    max_retries = 3
    retry_delay = 2
    
    for attempt in range(max_retries):
        try:
            async with MCPClient(MCP_URL) as mcp:
                await mcp.connect()

                # 1. CHECK CONSOLE ERRORS
                print(f"[*] Checking Unity console...")
                res = await mcp.call_tool("read_console", {"count": "100"})
                
                all_entries = []
                if res and "result" in res:
                    for item in res["result"].get("content", []):
                        if item.get("type") == "text":
                            data = DiagnosticsFormatter.parse_complex_data(item["text"])
                            if isinstance(data, dict):
                                all_entries.extend(data.get("data", []))
                
                # Filter false positives
                def is_real_error(e):
                    etype = e.get("type", "").upper()
                    msg = e.get("message", "")
                    if etype == "EXCEPTION" and "Saving results to" in msg:
                        return False
                    return etype in ["ERROR", "EXCEPTION", "ASSERT"]

                errors = [e for e in all_entries if is_real_error(e)]
                warnings = [e for e in all_entries if e.get("type", "").upper() == "WARNING"]
                logs = [e for e in all_entries if e.get("type", "").upper() == "LOG"]

                print(f"Console Stats: {Style.RED}{len(errors)} Errors{Style.RESET} | "
                      f"{Style.YELLOW}{len(warnings)} Warnings{Style.RESET} | "
                      f"{Style.CYAN}{len(logs)} Logs{Style.RESET}")
                
                if errors:
                    print(f"{Style.RED}{Style.BOLD}!!! CONSOLE ERRORS FOUND !!!{Style.RESET}")
                    print("=" * 60)
                    for i, entry in enumerate(errors):
                        print(DiagnosticsFormatter.format_log(entry))
                        if i < len(errors) - 1: print("-" * 40)
                    print("\n" + f"{Style.RED}Diagnostics aborted due to console errors.{Style.RESET}")
                    sys.exit(1)

                print(f"{Style.GREEN}[OK] Console is clean of errors.{Style.RESET}")

                # 2. SAVE SCENE
                print(f"[*] Saving current scene...")
                await mcp.call_tool("manage_scene", {"action": "save"})

                # 3. RUN TESTS
                print(f"[*] Running all EditMode tests...")
                res = await mcp.call_tool("run_tests", {"mode": "EditMode"})
                
                if res and res.get("result", {}).get("isError"):
                    print(f"{Style.RED}{Style.BOLD}!!! TEST RUN FAILED !!!{Style.RESET}")
                    for item in res["result"].get("content", []):
                        print(item.get("text", ""))
                    sys.exit(1)

                test_data = None
                if res and "result" in res:
                    for item in res["result"].get("content", []):
                        if item.get("type") == "text":
                            test_data = DiagnosticsFormatter.parse_complex_data(item["text"])
                elif res and res.get("method") == "notifications/message":
                    msg_text = res["params"]["data"].get("msg", "")
                    if msg_text.startswith("Response "):
                        test_data = DiagnosticsFormatter.parse_complex_data(msg_text[9:])

                if not test_data or not isinstance(test_data, dict):
                    print(f"{Style.YELLOW}Could not parse test results.{Style.RESET}")
                    print(json.dumps(res, indent=2))
                    return

                # 4. FINAL REPORT
                actual_test_data = test_data.get("data", test_data)
                tests = actual_test_data.get("results", [])
                passed = sum(1 for t in tests if t.get("state") == "Passed")
                failed = sum(1 for t in tests if t.get("state") == "Failed")
                total = len(tests)

                print("\n" + Style.BOLD + "FINAL DIAGNOSTIC REPORT" + Style.RESET)
                print("=" * 60)
                print(f"Console: {Style.GREEN}Clean{Style.RESET}")
                print(f"Tests:   {Style.GREEN}{passed} Passed{Style.RESET} | {Style.RED}{failed} Failed{Style.RESET} (Total: {total})")
                print("=" * 60)

                if failed > 0:
                    print(f"{Style.RED}{Style.BOLD}FAILED TESTS:{Style.RESET}")
                    for t in [t for t in tests if t.get("state") == "Failed"]:
                        print(DiagnosticsFormatter.format_test(t))
                        print("-" * 30)
                    sys.exit(1)
                else:
                    print(f"{Style.GREEN}{Style.BOLD}Verification Successful!{Style.RESET}")
                
                return # Success!

        except (httpx.ConnectError, httpx.TimeoutException) as e:
            if attempt < max_retries - 1:
                print(f"{Style.YELLOW}[!] MCP non-responsive (attempt {attempt+1}/{max_retries}). Retrying in {retry_delay}s...{Style.RESET}")
                await asyncio.sleep(retry_delay)
            else:
                print(f"{Style.RED}{Style.BOLD}[!] CRITICAL ERROR: MCP server at {MCP_URL} is unreachable or non-responsive.{Style.RESET}")
                print(f"{Style.RED}Ensure the MCP server is running and accessible.{Style.RESET}")
                print(f"{Style.DIM}Technical details: {e}{Style.RESET}")
                sys.exit(1)
        except Exception as e:
            print(f"{Style.RED}[!] Unexpected Error: {e}{Style.RESET}", file=sys.stderr)
            sys.exit(1)

if __name__ == "__main__":
    asyncio.run(run_diagnostics())
