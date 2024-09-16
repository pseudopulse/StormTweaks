# StormTweaks
Tweaks some of the content added by Seekers of the Storm.

## Chef
- Renamed to CHEF
- "Yes, Chef" skill renamed to "Second Helping"
- Second Helping cooldown reduced to 8 seconds.
- Sear no longer locks itself to your initial aim.
- All skills except Dice and Second Helping can be interrupted.
- Dice can now throw up to 3 knives at once, and all knives recall when you release M1 instead of requiring a second click.
- Roll now leaves a trail of oil when boosted by Second Helping
- Sear now goes the same distance as Artificer's Flamethrower (22m)

## False Son
- Lunar Spikes can now be charged up to fire a shotgun blast of up to 6. Damage: 150% -> 200%
- Lunar Spikes have slight seeking
- Laser of the Father uses the boss VFX (scaled appropriately), has a proper charge-up indicator, and no longer jiggles up and down
- Burst Laser now charges up a heavy beam that causes a large explosion for up to 2400% damage and stuns.
- Club of the Forsaken has smaller visual effects
- Club of the Forsaken is now agile
- Growth additionally factors in 1/8th of the max hp gained by leveling up (effectively +1 growth every 4 levels)

## Seeker
- Meditation now deals 800% damage.

## Build Instructions
Visual Studio 2022 Community Edition with .NET standard 2.1 can be used to build this project.

Additionally, please note that two Risk of Rain 2 official game assemblies need to be included within this project in order to build it from source. These two files are named `Decalicious.dll` and `Unity.Postprocessing.Runtime.dll`. You can find these assemblies within your game's installation directory. Example paths for these assemblies can be found below.
 - `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\Decalicious.dll`
 - `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\Unity.Postprocessing.Runtime.dll`

Including these assemblies within the existing project directory at `StormTweaks/StormTweaks` (adjacent to `Plugin.cs`) should automatically include them within Visual Studio's build chain.
