"""Send one command to this project's already-running Unity Pipeline server.

The local bearer token is read privately and is never written into logs or arguments.
Usage: python AgentScripts/NarrativePipeline.py command '{"parameter": "value"}'
"""
import json
import sys
import urllib.request
import urllib.error
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
root = Path(__file__).resolve().parent.parent
descriptor = json.loads((root / "Library/Pipeline/.unity-pipeline-port").read_text())
body = json.dumps({"command": sys.argv[1], "parameters": json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}}).encode()
request = urllib.request.Request(
    f"http://127.0.0.1:{descriptor['port']}/api/" + ("status" if sys.argv[1] == "--status" else "exec"), data=None if sys.argv[1] == "--status" else body,
    headers={"Authorization": "Bearer " + descriptor["evalToken"], "Content-Type": "application/json"},
)
try:
    with urllib.request.urlopen(request, timeout=55) as response:
        result = response.read().decode()
        print(result)
        if len(sys.argv) > 3:
            (root / sys.argv[3]).write_text(result, encoding="utf-8")
except urllib.error.HTTPError as error:
    print(error.read().decode())
    sys.exit(1)
