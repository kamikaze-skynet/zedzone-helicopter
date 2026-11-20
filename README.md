# Helicopter (ZED ZONE)

A flyable UH-1Y style helicopter with rocket pods and two door miniguns. No power grid or helipad needed.

## Requires

- ZED ZONE (Windows, Steam)
- [ZED ZONE Code Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3834614258) - run its install.cmd once, then start the game through Steam.

## How to use

| Key | What it does |
| --- | --- |
| **Home** | Call a helicopter. It lands a few metres in front of you. |
| **F** | Get in or out, like a car (you can't get out in the air). |
| Engine start | Start the engine from the driver's seat, like a car. |
| **Space** | Take off / land. Switching the engine off in the air also brings it down. |
| **Shift+Home** | Next to a landed helicopter (or sitting in it): pour every gasoline can you carry into its tank. |
| **WASD** | Fly. It flies over buildings and trees. Teammates can ride along. |
| **Hold left mouse** (passenger) | Door gunner: the first player riding as a passenger mans the left minigun, the second the right one. The gun turns to follow the mouse inside its door's arc. |
| **Hold left mouse** (pilot) | In flight, fires the rocket pods towards the mouse, left and right pod in turn. Each rocket uses one 84mm HE Round from the cargo hold or your backpack. |
| **Hold right mouse** (pilot) | In flight, fires the door gun on the mouse side, but only while no other player is aboard. |

The rockets are the game's own 84mm recoilless rounds and can't hurt the helicopter's crew. The guns lock on to the zombie nearest the pointer and use 5.56mm or 7.62x39mm rounds from the cargo hold or your backpack. Turn door gunners off with `DoorGunners = false` in the MachineGun section of the config.

- Each save can call in **1** helicopter by default (`MaxHelicopters`, 0 = no limit).
  You can't call another while one is parked within 40 m.
- It runs on **gasoline**, the same fuel as cars, and arrives with a full tank (about 15 minutes of flying). Refuel with Shift+Home and gasoline cans in your inventory.
- The rotor and the gun are loud: zombies within 30 m hear you.

## Building one

Open the Build menu and go to the **VEHICLES** tab. The UH-1Y Helicopter costs Iron Plate 80, Metal Component 30,
Copper Tube 15, Iron Wire 25 and Copper Wire 20, taken from what you carry; it lands a few tiles in front of you.
Door gun rounds give back their spent casings (5.56x45 Shell / 7.62x39 Shell); reload them at the reloader
(10 casings + 1 Gunpowder + 1 Copper Plate = 10 rounds).

Every rocket gives back an empty 84mm Rocket Case; at the reloader 1 case + 4 Gunpowder + 1 Iron Plate + 1 Metal Component
make a new 84mm HE Round (new cases: 2 Iron Plate at the reloader).

Co-op: everyone sees and hears each other's door gun and rocket shots (all players need this mod).
Rockets (84mm HE Round) are crafted at a workbench, 2 at a time, from Gunpowder 6, Iron Plate 2, Copper Tube 1 and Metal Component 1.

## Settings

`BepInEx\config\girthy_mods.zedzone.helicopter.cfg`, or in game with **Mod Menu (F11)**:
call key, max helicopters per save, speed, fuel use, gun damage / fire rate / spread / lock-on / sound,
rockets on/off, rocket ammo use, time between rockets, rocket damage,
effect size, volume, language.

## Industrial Age

Don't run both. If Industrial Age is installed, this mod turns itself off (it has its own
copy of the helicopter) and says so on screen. Helicopters made by Industrial Age stay
helicopters with this mod.

## Build

```
dotnet build src\Helicopter.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\ZED ZONE"
```

Add `-p:CopyToMods=true` to copy the DLL to `ZED ZONE\Mods\Helicopter`.
`publish-workshop.ps1 -SteamUser <login>` builds and uploads to the Steam Workshop.
