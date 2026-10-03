#!/usr/bin/env python3
"""Build the Thunderstore zip:  python3 tools/package.py [path/to/SiegeUs.dll]

Validates the manifest the same way Thunderstore does, then writes SiegeUs-<version>.zip:
  manifest.json, icon.png, README.md, CHANGELOG.md, plugins/SiegeUs.dll
"""
import json, os, re, struct, sys, zipfile

root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ts = os.path.join(root, "thunderstore")
dll = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "bin", "Release", "net6.0", "SiegeUs.dll")


def fail(msg):
    sys.exit("ERROR: " + msg)


if not os.path.exists(dll):
    fail(f"DLL not found: {dll}\nBuild first:  dotnet build -c Release")

man = json.load(open(os.path.join(ts, "manifest.json"), encoding="utf-8"))
for k in ("name", "version_number", "website_url", "description", "dependencies"):
    if k not in man:
        fail(f"manifest.json is missing '{k}'")
if not re.fullmatch(r"[A-Za-z0-9_]{1,128}", man["name"]):
    fail("name may only contain letters, digits and underscores (max 128)")
if not re.fullmatch(r"\d+\.\d+\.\d+", man["version_number"]):
    fail("version_number must be Major.Minor.Patch")
if not (1 <= len(man["description"]) <= 250):
    fail(f"description must be 1-250 chars (is {len(man['description'])})")
for d in man["dependencies"]:
    if not re.fullmatch(r"[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+", d):
        fail(f"bad dependency string '{d}' (expected Author-Name-1.2.3)")

# keep manifest and csproj versions in sync
csproj = open(os.path.join(root, "SiegeUs.csproj"), encoding="utf-8").read()
m = re.search(r"<Version>([^<]+)</Version>", csproj)
if m and m.group(1) != man["version_number"]:
    fail(f"version mismatch: csproj {m.group(1)} vs manifest {man['version_number']}")

# icon must be a 256x256 PNG
with open(os.path.join(ts, "icon.png"), "rb") as f:
    head = f.read(24)
if head[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", head[16:24]) != (256, 256):
    fail("icon.png must be a 256x256 PNG")

out = os.path.join(root, f"SiegeUs-{man['version_number']}.zip")
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for f in ("manifest.json", "icon.png", "README.md", "CHANGELOG.md"):
        z.write(os.path.join(ts, f), f)
    z.write(dll, "plugins/SiegeUs.dll")
print("wrote", out)
