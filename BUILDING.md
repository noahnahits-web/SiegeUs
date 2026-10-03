# Building Siege Us

## Easiest: let GitHub build it (no SDK needed)
Push this folder to a GitHub repo. The workflow in `.github/workflows/build.yml` builds the DLL and
produces `SiegeUs-0.1.0.zip` as a downloadable artifact (Actions tab -> latest run -> Artifacts).
Game libraries come from the public BepInEx NuGet feed, so you do not need the game installed.

## Build locally
1. Install the .NET 6 SDK (or newer).
2. `dotnet build -c Release`
   - If `<Among Us>\BepInEx\plugins` exists, the DLL is copied there automatically.
     Override the game folder with `-p:AmongUsDir="D:\Games\Among Us"`.
3. `python3 tools/package.py` -> `SiegeUs-0.1.0.zip`

## Test before uploading
1. Install the zip with r2modman / Thunderstore App (Import local mod) on **two** machines or two game instances.
2. Launch, open the BepInEx log and look for: `Siege Us: 73 operators, 107 weapons, 0 data errors.`
3. Host a lobby, join with the second client, pick opposite teams, host presses START MATCH.

## Upload to Thunderstore
- thunderstore.io -> Among Us community -> Upload. Choose your team as the author.
- Dependency is `BepInEx-BepInExPack_AmongUs-6.0.700` (already in `thunderstore/manifest.json`).
  If the game updates and a newer pack is published, bump it there.
- Optional: put your repo URL in `website_url`.
- Bump `<Version>` in `SiegeUs.csproj` AND `version_number` in `thunderstore/manifest.json` together
  (package.py refuses to build if they differ).

## Editing content
- Operators:  src/Core/OperatorDatabase.cs   (one line each)
- Weapons:    src/Core/WeaponDatabase.cs
- Oregon:     tools/gen_oregon.py  ->  python3 tools/gen_oregon.py  (regenerates maps/oregon.json)

## Game-version-sensitive spots (check these first if a game update breaks the mod)
These touch Among Us internals that change between versions. Compare names against `BepInEx/interop`:
- `SiegeNet.cs`   `PlayerControl.HandleRpc(byte, MessageReader)` patch, `StartRpcImmediately` / `FinishRpcImmediately`
- `SiegeGame.cs`  `PlayerControl.moveable`, `.Visible`, `MyPhysics.Speed`, `NetTransform.RpcSnapTo`, `AmongUsClient.GameId`
- `MapBuilder.cs` the "Ship" physics layer used for wall collision
- `SiegeUI.cs`    IMGUI (OnGUI) needs the unstripped UnityEngine.IMGUIModule, which BepInEx generates on first run
