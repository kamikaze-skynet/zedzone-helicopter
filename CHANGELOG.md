# Changelog

## 1.3.1
- Fixed a tank turning into a helicopter while you drive it with the Tank mod loaded. The helicopter now leaves other mods' vehicles alone, and checks with the Tank mod before it claims a vehicle after a reload.

## 1.3.0
- Spent casings come back: every door gun round gives back its 5.56x45 Shell or 7.62x39 Shell (onto a stack in your bag, else the cargo hold, else a new spot in your bag).
- If nothing reloads those rounds from casings yet (the Tank mod adds the same recipes), the reloader gets one: 10 casings + 1 Gunpowder + 1 Copper Plate = 10 rounds.
- Rockets start clear of the helicopter and can't damage it or its crew.

## 1.2.0
- Build it: the helicopter is in the Build menu on a VEHICLES tab (shared with the Tank mod). BUILD takes Iron Plate 80, Metal Component 30, Copper Tube 15, Iron Wire 25 and Copper Wire 20 from what you carry and the helicopter lands in front of you.
- Rockets can be crafted: 84mm HE Round x2 at a workbench from Gunpowder 6, Iron Plate 2, Copper Tube 1, Metal Component 1 (if the game has no recipe of its own).
- Rocket pods: the pilot fires rockets towards the mouse with the left mouse button (the game's 84mm recoilless rounds, one 84mm HE Round each from the cargo hold or backpack). They can't hurt the helicopter's crew.
- The pilot's door gun moved to the right mouse button (still only while no other player is aboard).
- Door gun sound fixed: it was silent because the game loads gun sounds on demand. It now uses the M249's sound by default (config MachineGun / SoundFromWeapon) and every round plays, so a held trigger sounds like a machine gun.

## 1.1.0
- New look: a realistic UH-1Y Venom made in Blender, modelled from a photo (gray Marine paint, tall transmission hump, big exhausts, open cabin door with empty troop seats, sliding and hinged doors, GAU-17 miniguns on both sides, rocket pods, FLIR, four-blade rotor, tail rotor on the left of the fin, antennas, flare dispensers, missile warning sensors, searchlight, cargo hook, drive shaft hangers, rotor trim tabs, exhaust soot and grime). Real Marine markings: low-vis national insignia, MARINES, MS / 169104 / UH-1Y on the fin, big "08" on the nose sides, nose badges, rescue arrows, cut-here corners, NO STEP and walkway stencils, red/white tail rotor tips. Cockpit doors like the real one: hinged on a slanted front edge, with a slanted front pane and a tall rear pane with a vent slider; the windscreen side pane now runs back to the door. Side skin detail: panel seams, rivet rows, access panels, vent grille, placards, step and grab handle. Engine air intakes with lips and screens. The gunner doors ahead of the cabin openings are swung open and latched against the body, and the miniguns now sit in those openings. Nose rebuilt as one smooth piece matched to photos: squared windscreen with framed glass, real cockpit doors and window frames, and chin windows that wrap under the nose for seeing the ground. Source scene in blender/UH1Y.blend.
- Door gunners: each player riding as a passenger mans a door minigun (1st passenger left, 2nd right), aimed with the mouse inside that door's arc. The guns turn on screen. The pilot fires the gun on the mouse side only while no other player is aboard. Config: MachineGun / DoorGunners.


## 1.0.0
- First standalone release.
- No power grid or helipad: press Home to call one in (1 per save by default).
- Turns itself off when Industrial Age is installed, so the two don't fight over the same helicopter.
- HeliApi.CallIn for other mods: Spawn Menu's Helicopter kit calls one in (skips the per-save limit).
- Gasoline: Shift+Home next to a landed helicopter pours your gasoline cans into it (the game's own car refuel code). HeliApi.GiveFuelCans gives full cans (Spawn Menu kit).
- Switching the engine off in flight now makes it come down instead of flying on.
- Own config file (girthy_mods.zedzone.helicopter.cfg), works with Mod Menu (F11).
