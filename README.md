# BackpackGunSlot

Adds a gun slot to backpacks, shown as an **ON PACK** slot next to the backpack on the gear screen.
This branch targets **SPT 5.0** (IL2CPP, BepInEx 6).

The slot size follows the backpack size, so bigger packs take bigger guns:

| Backpack cells | Takes |
| --- | --- |
| up to 12 | pistols |
| up to 18 | + SMGs |
| up to 24 | + shotguns |
| up to 35 | + assault rifles, carbines, marksman rifles |
| larger | + sniper rifles |

## Install

Extract `BackpackGunSlot-<version>-SPT5.0.zip` into your SPT folder. It contains:

- `BepInEx/plugins/BackpackGunSlot/BackpackGunSlot.dll` — the client plugin that draws the slot
- `SPT_Runtime/user/mods/BackpackGunSlot/` — the server mod that adds the slot to backpack templates, and its `config/config.json`

Both parts are required.

## Configuration

Server (`SPT_Runtime/user/mods/BackpackGunSlot/config/config.json`):

- `enabled` — turn the slot off without uninstalling
- `slotName` — internal slot name; must match the client's `Slot name` setting
- `allowAnyGun` — every backpack takes every weapon class
- `tiers` — cell limits and the weapon classes each tier allows: `Pistol`, `SMG`, `Shotgun`, `AssaultRifle`,
  `AssaultCarbine`, `MarksmanRifle`, `SniperRifle`, `MachineGun`, `GrenadeLauncher`, `SpecialWeapon`
- `overrides` — per-backpack template id: a comma-separated class list, or `none` to leave that pack without a slot

Client (F12 menu or `BepInEx/config/com.20fpsguy.backpackgunslot.cfg`): enable/disable, slot name, slot caption, debug logging.

## Building

Requires the .NET 10 SDK and an SPT 5.0 install that has been started once, so BepInEx has generated `BepInEx/interop`.

```powershell
./build-release.ps1 -SptRoot "D:\SPT 5.0"
```

This builds both projects and writes `release/BackpackGunSlot-<version>-SPT5.0.zip`. If the SPT server assemblies
(`SPTarkov.Server.Core.dll` etc.) are not in `<SptRoot>\SPT_Runtime`, pass `-SptServerDir`.

## License

MIT. Original mod by 20fpsguy.
