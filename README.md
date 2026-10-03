# Siege Us

A top-down, round-based tactical shooter mode for Among Us, inspired by Rainbow Six Siege.

**Every player in the lobby must have this mod installed** (same version). No other mods or libraries are required besides BepInEx, which the mod manager installs for you.

## How to play
1. Everyone joins the same lobby (a private lobby works best).
2. Each player picks **Blue** or **Orange** in the Siege Us panel on the left (press **F9** to hide/show it). Max 5 per team.
3. The host adjusts rounds-to-win and options, then presses **START MATCH**.
4. Each round: **spawn / bomb-site vote -> operator select -> prep -> action -> round end**. First team to 8 round wins (configurable 1-12) takes the match. Sides swap every round by default.
5. When the match ends the host presses **BACK TO LOBBY**. Use that before starting a normal Among Us game.

## Features
- Attackers vs Defenders. Attackers vote their **spawn**, defenders vote the **bomb site**.
- **73 operators** (unique-per-team picks, Recruit can duplicate) with primary/secondary choice.
- **107 guns** with per-gun damage, fire rate, magazine, reload time and spread.
- **Oregon** blockout: basement + 3 floors, soft / reinforceable / hard walls, glass, soft floors and ceilings, stairs.
- Plant / defuse the bomb, reinforce walls (defenders, prep), melee-breach walls and soft floors/ceilings, switch floors via stairs and broken hatches.

## Controls
| Key | Action |
|---|---|
| Move | Normal Among Us movement (**use keyboard movement**, not click-to-move, since left click fires) |
| Mouse 1 | Fire |
| 1 / 2 | Primary / secondary |
| R | Reload |
| V | Melee (breaks soft walls, hits players, breaches soft floors) |
| E / Q | Up / down through stairs or broken hatches |
| T | Reinforce nearest wall (defenders, prep phase) |
| F (hold 7s) | Plant (attackers) / Defuse (defenders) inside the bomb site |
| F9 | Show / hide the lobby panel |

## Status
Early alpha. Operator gadgets and abilities are listed in the picker but **not implemented yet**. Weapon stats are approximations tuned for top-down play.

## Troubleshooting
- Check `BepInEx/LogOutput.log` for the line `Siege Us: 73 operators, 107 weapons, 0 data errors.` If it is missing the mod did not load.
- If the game updated and the mod stops working, wait for a mod update (or rebuild against the new game version).
- Everyone must be on the same Siege Us version; mismatched snapshots are rejected and logged.

## Credits
Rainbow Six Siege is a trademark of Ubisoft. This is an unofficial fan project and uses no Ubisoft assets. Not affiliated with Innersloth. Built on BepInEx.
