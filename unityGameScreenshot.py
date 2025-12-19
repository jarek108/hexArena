import asyncio
from mcp.client.streamable_http import streamable_http_client
from mcp import ClientSession
import os
import shutil
import json

async def screen():
     # this uses MCP-unity server for the screenshot and moves it outside the Assets folder
    url = "http://localhost:8080/mcp"

    async with streamable_http_client(url) as (read_stream, write_stream, _): 
        async with ClientSession(read_stream, write_stream) as session:
            await session.initialize()

            result = await session.call_tool("manage_scene", arguments={"action": "screenshot"})
            path = json.loads(result.content[0].text)['data']['fullPath']
            
            # Move the file to hexagon/Screenshots
            filename = os.path.basename(path)
            dest_dir = os.path.join("hexagon", "Screenshots")
            os.makedirs(dest_dir, exist_ok=True)
            dest_path = os.path.join(dest_dir, filename)
            
            shutil.move(path, dest_path)
            
            # Delete the meta file if it exists
            meta_path = path + ".meta"
            if os.path.exists(meta_path):
                os.remove(meta_path)
                
            print(dest_path)

if __name__ == "__main__":
    asyncio.run(screen())