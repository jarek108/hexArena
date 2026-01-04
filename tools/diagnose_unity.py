"""
Unity Dynamic Diagnostics Tool
------------------------------
Automates the Unity development feedback loop by:
1. Auditing the Unity Console for critical errors/exceptions.
2. Waiting for background compilation (polling DLL vs CS timestamps).
3. Saving the scene and running all EditMode tests.

Usage:
    python diagnose_unity.py                # Full run: Console -> Wait -> Tests
"""

import asyncio
import httpx
import json
import sys
import os
import glob
import argparse
from datetime import datetime
from typing import Optional, Dict, Any, List

# --- Configuration ---
MCP_URL = "http://localhost:8080/mcp"
UNITY_PROJECT_PATH = r"../"
SCAN_INTERVAL = 2
BUILD_STABILITY_DELAY = 60  # Seconds to wait after any DLL is updated to ensure completion of all DLLs

# --- Derived Paths ---
CS_FOLDER_PATH = os.path.join(UNITY_PROJECT_PATH, "Assets", "Scripts")
DLL_FOLDER_PATH = os.path.join(UNITY_PROJECT_PATH, "Library")

TESTS_STARTED_MSG = "Executing IPrebuildSetup for: Unity.PerformanceTesting.Editor.TestRunBuilder"
TESTS_ENDED_MSG = "Executing IPostBuildCleanup for: Unity.PerformanceTesting.Editor.TestRunBuilder"

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

    def get_file_timestamps(self, path: str, extension: str) -> Dict[str, float]:
        timestamps = {}
        search_pattern = os.path.join(path, "**", f"*{extension}")
        files = glob.glob(search_pattern, recursive=True)
        for file_path in files:
            try:
                timestamps[file_path] = os.path.getmtime(file_path)
            except OSError:
                continue
        return timestamps

    async def wait_for_compilation(self):
        print(f"[*] Monitoring compilation status...")
        start_time = datetime.now()
        last_file_count = -1
        
        while True:
            now_ts = datetime.now().timestamp()
            cs_timestamps = self.get_file_timestamps(CS_FOLDER_PATH, ".cs")
            dll_timestamps = self.get_file_timestamps(DLL_FOLDER_PATH, ".dll")
            
            newest_cs_time = max(cs_timestamps.values()) if cs_timestamps else 0
            newest_dll_time = max(dll_timestamps.values()) if dll_timestamps else 0
            
            # Check if build is logically up to date
            is_newer = newest_dll_time >= newest_cs_time
            # Check if build is "old enough" to be considered stable/finished
            dll_age = now_ts - newest_dll_time
            is_stable = dll_age >= BUILD_STABILITY_DELAY

            if is_newer:
                if is_stable:
                    if last_file_count != -1: print() # Line break after waiting
                    print(f"{Style.GREEN}[OK] Build is up to date and stable (DLL age: {int(dll_age)}s).{Style.RESET}")
                    break
                else:
                    print(f"{Style.CYAN}      Waiting to ensure build completion... ({int(BUILD_STABILITY_DELAY - dll_age)}s remaining){Style.RESET}", end="\r")
                    await asyncio.sleep(1)
                    continue

            # If not up to date, handle file listing and timer
            changed_files = [os.path.relpath(f, UNITY_PROJECT_PATH) for f, t in cs_timestamps.items() if t > newest_dll_time]
            changed_files.sort()
            
            elapsed = datetime.now() - start_time
            mm, ss = divmod(int(elapsed.total_seconds()), 60)
            wait_timer = f"{mm}:{ss:02d}"
            
            if len(changed_files) != last_file_count:
                if last_file_count != -1: print()
                print(f"{Style.YELLOW}[WAIT] {len(changed_files)} files modified since last build:{Style.RESET}")
                for f in changed_files:
                    print(f"\t{f}")
                last_file_count = len(changed_files)

            print(f"{Style.YELLOW}      Waiting {wait_timer}...{Style.RESET}", end="\r")
            await asyncio.sleep(SCAN_INTERVAL)

    async def check_console_errors(self):
        print(f"[*] Checking Unity console...")
        
        for attempt in range(11): # 0 to 10 seconds
            res = await self.mcp.call_tool("read_console", {"count": "500", "format": "json"})
            all_entries = []
            if res and "result" in res:
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

            # State Detection (Checking Log, Warning, and Exception)
            # all_logs = logs + warnings + [e for e in all_entries if str(e.get("type", "")).upper() == "EXCEPTION"]
            # starts = [l for l in all_logs if TESTS_STARTED_MSG in str(l.get("message", ""))]
            # ends = [l for l in all_logs if TESTS_ENDED_MSG in str(l.get("message", ""))]
            
            # status_msg = ""
            # if not starts and not ends:
            #     status_msg = f"{Style.DIM}no testing history detected, moving on{Style.RESET}"
            # else:
            #     if len(starts) > len(ends):
            #         status_msg = f"{Style.YELLOW}last tests started, still not finished{Style.RESET}"
            #         if attempt < 10:
            #             print(f"      waiting for the tests to finish ({10 - attempt}s) | waiting since {attempt}s | {status_msg}", end="\r")
            #             await asyncio.sleep(1)
            #             continue
            #     else:
            #         status_msg = f"{Style.CYAN}last test finished, moving on{Style.RESET}"
            
            # if attempt > 0 or status_msg: 
            #     if attempt > 0: print() # Clear the 'waiting' line
            #     print(f"      {status_msg}")
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
        
        print(f"[*] Running EditMode tests...")
        res = await self.mcp.call_tool("run_tests", {"mode": "EditMode", "timeout_seconds": "300"})
        
        test_data = self.parse_test_response(res)
        if not test_data:
            print(f"{Style.RED}Could not parse test results.{Style.RESET}")
            sys.exit(1)

        results = self.extract_results(test_data)
        self.report_and_exit(results)

    def parse_test_response(self, res):
        if res and "result" in res:
            content = res["result"].get("content")
            if content:
                for item in content:
                    if item.get("type") == "text":
                        return DiagnosticsFormatter.parse_complex_data(item["text"])
        elif res and res.get("method") == "notifications/message":
            params = res.get("params")
            if params and "data" in params:
                msg_text = params["data"].get("msg", "")
                if msg_text.startswith("Response "):
                    return DiagnosticsFormatter.parse_complex_data(msg_text[9:])
        return None

    def extract_results(self, test_data):
        if isinstance(test_data, dict) and "data" in test_data:
            actual = test_data["data"]
        else:
            actual = test_data
        
        if isinstance(actual, dict): return actual.get("results", [])
        if isinstance(actual, list): return actual
        return []

    def report_and_exit(self, results):
        passed = sum(1 for t in results if isinstance(t, dict) and t.get("state") == "Passed")
        failed = sum(1 for t in results if isinstance(t, dict) and t.get("state") != "Passed")
        total = len(results)

        print(f"\n{Style.BOLD}DIAGNOSTIC REPORT{Style.RESET}")
        print(f"Tests: {Style.GREEN}{passed} Passed{Style.RESET} | {Style.RED}{failed} Failed{Style.RESET} (Total: {total})")
        
        if total == 0:
            print(f"{Style.RED}{Style.BOLD}[!] ERROR: No tests were found.{Style.RESET}")
            sys.exit(1)

        if failed > 0:
            print(f"{Style.RED}{Style.BOLD}FAILED TESTS:{Style.RESET}")
            for t in [t for t in results if isinstance(t, dict) and t.get("state") == "Failed"]:
                print(DiagnosticsFormatter.format_test(t))
            sys.exit(1)
        
        print(f"{Style.GREEN}{Style.BOLD}Verification Successful!{Style.RESET}")

    async def execute(self):
        os.system('') # Enable ANSI
        async with self.mcp:
            await self.mcp.connect()
            await self.check_console_errors()
            await self.wait_for_compilation()
            await self.run_tests()

# --- Utility Classes (MCPClient & Formatters unchanged logic, just moved) ---
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
            resp = await self.client.get(self.base_url, headers={"Accept": "application/json"})
            self.sid = resp.headers.get("mcp-session-id") or resp.json().get("sessionId")
            await self.rpc("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "unity-diag", "version": "1.0"}}, msg_id=1)
            await self.rpc("notifications/initialized")
        except httpx.RequestError:
            print(f"{Style.RED}{Style.BOLD}[!] ERROR: Could not connect to MCP server at {self.base_url}.{Style.RESET}")
            print(f"{Style.YELLOW}    Make sure the Unity project is open and the MCP server is running.{Style.RESET}")
            sys.exit(1)
        except Exception as e:
            print(f"{Style.RED}{Style.BOLD}[!] ERROR: Connection failed: {e}{Style.RESET}")
            sys.exit(1)

    async def rpc(self, method: str, params: Optional[Dict] = None, msg_id: Optional[int] = None):
        payload = {"jsonrpc": "2.0", "method": method}
        if params is not None: payload["params"] = params
        if msg_id is not None: payload["id"] = msg_id
        url = f"{self.base_url}?sessionId={self.sid}"
        headers = {"mcp-session-id": self.sid, "Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        r = await self.client.post(url, json=payload, headers=headers)
        if msg_id is None: return None
        for line in r.text.splitlines():
            if line.startswith("data: "):
                data = json.loads(line[6:])
                if data.get("id") == msg_id: return data
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
    args = parser.parse_args()

    asyncio.run(UnityDiagnostics().execute())