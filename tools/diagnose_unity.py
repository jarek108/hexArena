"""
Unity Dynamic Diagnostics Tool
------------------------------
Automates the Unity development feedback loop by:
1. Establishing an MCP Handshake for session management.
2. Triggering a native Unity Asset Database Refresh and Compilation.
3. Auditing the Unity Console for critical errors/exceptions.
4. Saving the scene and running EditMode tests via Async Job API.

Usage:
    python diagnose_unity.py                # Full run: Handshake -> Refresh -> Console -> Tests
    python diagnose_unity.py --debug        # Dev run: Skips tests for faster iteration
"""

import asyncio
import httpx
import json
import sys
import os
import argparse
import subprocess
from datetime import datetime
from typing import Optional, Dict, Any, List

# --- Configuration ---
MCP_URL = "http://localhost:8080/mcp"
UNITY_PROJECT_PATH = r"../"
UNITY_WINDOW_TITLE = "unity 6.3"

class Style:
    RED = "\033[91m"
    YELLOW = "\033[93m"
    CYAN = "\033[96m"
    GREEN = "\033[92m"
    BOLD = "\033[1m"
    DIM = "\033[2m"
    UNDERLINE = "\033[4m"
    RESET = "\033[0m"

class UnityDiagnostics:

    def __init__(self):
        self.mcp = MCPClient(MCP_URL)

    async def wait_for_compilation(self):
        # Compilation is now handled by refresh_unity call in execute()
        return

    async def check_console_errors(self):
        print(f"[*] Checking Unity console...")
        
        for attempt in range(11): # 0 to 10 seconds
            res = await self.mcp.call_tool("read_console", {"count": "500", "format": "json"})
            
            if not res:
                print(f"{Style.RED}[!] Failed to get response from read_console (empty response).{Style.RESET}")
                sys.exit(1)
            
            if "error" in res:
                print(f"{Style.RED}[!] MCP Tool Error: {res['error'].get('message')}{Style.RESET}")
                sys.exit(1)

            all_entries = []
            if "result" in res:
                content = res["result"].get("content")
                if content:
                    for item in content:
                        if item.get("type") == "text":
                            data = DiagnosticsFormatter.parse_complex_data(item.get("text", ""))
                            if isinstance(data, dict):
                                if "data" in data and isinstance(data["data"], list):
                                    all_entries.extend(data["data"])
                                elif "entries" in data and isinstance(data["entries"], list):
                                    all_entries.extend(data["entries"])
                            elif isinstance(data, list):
                                all_entries.extend(data)

            def is_real_error(e):
                etype = str(e.get("type", "")).upper()
                msg = str(e.get("message", ""))
                if "WebSocket" in msg and "Connection failed" in msg: return False
                if "Undo after editor test run" in msg: return False
                if "UnityConnectWebRequestException" in msg: return False
                if etype == "EXCEPTION" and "Saving results to" in msg: return False
                return etype in ["ERROR", "EXCEPTION", "ASSERT"]

            errors = [e for e in all_entries if is_real_error(e)]
            warnings = [e for e in all_entries if str(e.get("type", "")).upper() == "WARNING"]
            logs = [e for e in all_entries if str(e.get("type", "")).upper() == "LOG"]
            break

        print(f"      Console Stats: {Style.RED}{len(errors)} Errors{Style.RESET} | "
              f"{Style.YELLOW}{len(warnings)} Warnings{Style.RESET} | "
              f"{Style.CYAN}{len(logs)} Logs{Style.RESET}")

        if errors:
            print(f"      {Style.RED}{Style.BOLD}!!! CONSOLE ERRORS FOUND !!!{Style.RESET}")
            for entry in errors:
                print(DiagnosticsFormatter.format_log(entry, indent="      "))
            sys.exit(1)
        print(f"      {Style.GREEN}[OK] Console is clean.{Style.RESET}")

    async def run_tests(self):
        print(f"[*] Saving scene...")
        await self.mcp.call_tool("manage_scene", {"action": "save"})
        
        print(f"[*] Starting EditMode tests (Async)...")
        res = await self.mcp.call_tool("run_tests_async", {"mode": "EditMode"})
        
        if not res or "result" not in res:
            print(f"{Style.RED}Failed to start async tests. Raw response: {res}{Style.RESET}")
            sys.exit(1)
            
        content = res["result"].get("content")
        job_id = None
        if content:
            for item in content:
                if item.get("type") == "text":
                    data = DiagnosticsFormatter.parse_complex_data(item["text"])
                    if isinstance(data, dict) and "data" in data:
                        job_id = data["data"].get("job_id")
        
        if not job_id:
            print(f"{Style.RED}Could not extract job_id from response.{Style.RESET}")
            sys.exit(1)

        print(f"[*] Test job started: {job_id}")
        
        while True:
            await asyncio.sleep(2)
            res = await self.mcp.call_tool("get_test_job", {"job_id": job_id})
            
            if not res or "result" not in res:
                continue

            content = res["result"].get("content")
            if not content: continue
            
            job_data = None
            for item in content:
                if item.get("type") == "text":
                    parsed = DiagnosticsFormatter.parse_complex_data(item["text"])
                    if isinstance(parsed, dict) and "data" in parsed:
                        job_data = parsed["data"]
            
            if not job_data: continue
            
            status = job_data.get("status")
            progress = job_data.get("progress", {})
            completed = progress.get("completed", 0)
            total = progress.get("total", "?")
            
            print(f"{Style.CYAN}      Progress: {completed}/{total} tests completed...{Style.RESET}", end="\r")
            
            if status in ["succeeded", "failed"]:
                print() # New line after progress
                result_data = job_data.get("result")
                if result_data:
                    self.report_and_exit_async(result_data)
                else:
                    print(f"{Style.RED}Job finished but no result data found.{Style.RESET}")
                    sys.exit(1)
                break
            elif status == "error":
                print(f"\n{Style.RED}[!] Test Job Error: {job_data.get('error')}{Style.RESET}")
                sys.exit(1)

    def report_and_exit_async(self, result_data):
        summary = result_data.get("summary", {})
        passed = summary.get("passed", 0)
        failed = summary.get("failed", 0)
        total = summary.get("total", 0)

        print(f"\n{Style.BOLD}DIAGNOSTIC REPORT{Style.RESET}")
        print(f"Tests: {Style.GREEN}{passed} Passed{Style.RESET} | {Style.RED}{failed} Failed{Style.RESET} (Total: {total})")
        
        if total == 0:
            print(f"{Style.RED}{Style.BOLD}[!] ERROR: No tests were found.{Style.RESET}")
            sys.exit(1)

        if failed > 0:
            print(f"{Style.RED}{Style.BOLD}FAILED TESTS: {failed}{Style.RESET}")
            sys.exit(1)
        
        print(f"{Style.GREEN}{Style.BOLD}Verification Successful!{Style.RESET}")

    async def execute(self, debug_mode: bool = False):
        os.system('') # Enable ANSI

        # Focus Unity window
        activator_path = os.path.join("tools", "window_activator.py")
        if os.path.exists(activator_path):
            print(f"[*] Focusing Unity window ({UNITY_WINDOW_TITLE})...")
            try:
                subprocess.run([sys.executable, activator_path, UNITY_WINDOW_TITLE], check=False)
            except Exception as e:
                print(f"{Style.YELLOW}[!] Failed to run window activator: {e}{Style.RESET}")
        else:
            print(f"{Style.YELLOW}[!] Warning: {activator_path} not found. Skipping window focus.{Style.RESET}")

        async with self.mcp:
            print(f"[*] Establishing Handshake...")
            await self.mcp.connect()
            
            print(f"[*] Triggering Unity Refresh/Compilation...")
            await self.mcp.call_tool("refresh_unity", {"compile": "request", "mode": "if_dirty", "scope": "all", "wait_for_ready": True})
            
            await self.check_console_errors()
            await self.wait_for_compilation()
            
            # NOTE: debug_mode skips tests; only use for development/testing iterations
            if debug_mode:
                print(f"[*] Skipping tests (--debug active)...")
                print(f"{Style.GREEN}{Style.BOLD}Verification Successful! (Tests Skipped){Style.RESET}")
            else:
                await self.run_tests()

# --- Utility Classes ---
class MCPClient:
    def __init__(self, base_url: str):
        self.base_url = base_url
        self.sid, self.client = None, None

    async def __aenter__(self):
        self.client = httpx.AsyncClient(timeout=30000.0)
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.client: await self.client.aclose()

    async def connect(self):
        try:
            # We initialize directly via POST. The server establishes the session 
            # and provides the mcp-session-id in the response headers.
            self.sid = None 
            
            await self.rpc("initialize", {
                "protocolVersion": "2024-11-05", 
                "capabilities": {}, 
                "clientInfo": {"name": "unity-diag", "version": "1.0"}
            }, msg_id=1)
            
            await self.rpc("notifications/initialized")
        except Exception as e:
            print(f"{Style.RED}{Style.BOLD}[!] ERROR: Connection failed: {e}{Style.RESET}")
            sys.exit(1)

    async def rpc(self, method: str, params: Optional[Dict] = None, msg_id: Optional[int] = None):
        payload = {"jsonrpc": "2.0", "method": method}
        if params is not None: payload["params"] = params
        if msg_id is not None: payload["id"] = msg_id
        
        url = self.base_url
        if self.sid:
            url += f"?sessionId={self.sid}"
            
        headers = {
            "Content-Type": "application/json", 
            "Accept": "application/json, text/event-stream"
        }
        if self.sid:
            headers["mcp-session-id"] = self.sid
            
        r = await self.client.post(url, json=payload, headers=headers)
        
        if not self.sid:
            self.sid = r.headers.get("mcp-session-id")
            if not self.sid:
                try: self.sid = r.json().get("sessionId")
                except: pass
        
        # Log successful actions, but suppress repetitive polling for a cleaner UI
        if method != "notifications/initialized":
            is_polling = (method == "tools/call" and params and params.get("name") == "get_test_job")
            if not is_polling or r.status_code >= 400:
                print(f"      [NET] POST {method} -> {r.status_code}")
            
        if msg_id is None: return None
        for line in r.text.splitlines():
            if line.startswith("data: "):
                try:
                    data = json.loads(line[6:])
                    if data.get("id") == msg_id: return data
                except: continue
        return None

    async def call_tool(self, name: str, args: Optional[Dict[str, Any]] = None):
        return await self.rpc("tools/call", {"name": name, "arguments": args or {}}, msg_id=2)

class DiagnosticsFormatter:
    @staticmethod
    def format_log(e, indent=""):
        color = { "ERROR": Style.RED, "WARNING": Style.YELLOW, "LOG": Style.CYAN }.get(str(e.get("type","")).upper(), Style.RESET)
        msg = f"{color}{Style.BOLD}[{e.get('type','').upper()}]{Style.RESET} {e.get('file','')}:{e.get('line',0)}\n{e.get('message','')}"
        return "\n".join([f"{indent}{line}" for line in msg.splitlines()])

    @staticmethod
    def format_test(t):
        color = Style.GREEN if t.get("state") == "Passed" else Style.RED
        return f"{color}[{t.get('state','').upper()}]{Style.RESET} {t.get('fullName','Test')}\n  {t.get('message','')}"

    @staticmethod
    def parse_complex_data(input_data):
        if not isinstance(input_data, str): return input_data
        try: return json.loads(input_data)
        except: return input_data

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Unity Dynamic Diagnostics Tool")
    parser.add_argument("--debug", action="store_true", help="Debug mode: Skip the test run phase")
    args = parser.parse_args()

    asyncio.run(UnityDiagnostics().execute(debug_mode=args.debug))