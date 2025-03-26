### Thank you for being interested in contributing to Project: Lotus, here's how you can do that!

# Table Of Contents
1. [Information](#1---information)
2. [Getting Started](#2---getting-started)
3. [Roles](#3---roles)

## 1 - Information

This section details general information about Project Lotus.
Starting off, Project Lotus works completely differently to any other host only mods right now.
_Most_ host only mods are forked from [Town Of Host](https://github.com/tukasa0001/TownOfHost/), while this one is not.
As a result, any knowledge about TOH's codebase will not help you here.

## 2 - Getting Started

This guide assumes you already have everything required to build an Among Us Mod.
If this is not the case, I recommend looking at [Reactor's guide](https://docs.reactor.gg/quick_start/install_netsdk_template) at modding.
Stop when you get to the __Mod Template__.

In order to compile the mod, you need to first compile or download [Vent Framework](https://github.com/Lotus-AU/VentFramework-Continued) if you are building Debug.
If you are building release, the version on Nuget should be fine, but if it's not, you'll have to go through [Github](https://github.com/Lotus-AU/VentFramework-Continued).

You'll also need to edit the `AmongUs` varible in the csproj file.
```
<AmongUs>E:\SteamLibrary\steamapps\common\Among Us</AmongUs>
```
Change this to your Among Us folder, or remove it if you have it set in your Environmental variables.

## 3 - Roles

This section goes over how to add your own custom roles.
The role code in Project Lotus is very easy once you get the hang of it.
To start, check out somes roles at [src/Roles/RoleGroups](../src/Roles/RoleGroups/) to get a grasp at how to make your own role.

Here are some keypoints of the Role Code:
```csharp
[RoleAction(LotusActionType.EnterVent)]
// The function this attribute is applied to will run when the target action (EnterVent in this case) is done.
// You can also add Action Flags (Unblockable, WorksAfterDeath) that affect when the action runs.
// Some actions have parameters. You can view the LotusActionType file 
//     (src/Roles/Internals/Enums/LotusActionType) for all of it's parameters.
// You can pass an ActionHandle into every ActionType.
// Using an ActionHandle, you able to call .Cancel() which will prevent the action from running. 
//     (e.g. preventing shapeshift on LotusAction.Shapeshift)

protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream);
// This is pretty self explanatory.
// It might look very daunting at first, but after you get the hang of how GameOptionBuilder works, it's pretty easy.
// This is where all your options should be generated.

protected override RoleModifier Modify(RoleModifier roleModifier);
// This function *modifies* your role.
// In this function, you edit stuff about the role like it's color, faction, and ability flags.
// This function is ran a second time when the role is assigned, so you can reference options to edit the role for a game.

[Localized(nameof(Bastion))]
internal static class BastionTranslations
// This is important.
// While not needed, the Localized attribute adds your role's strings to the .yaml localization file.
// This is good because it allows us to localize your role for all of our supported languages.
```

In order to add your role to a GameMode, head over to [src/GameModes](../src/Gamemodes/).
Each Gamemode should have a file associated with the roles of it's gamemodes.
Create a new Instance of your role in the Role class of your target gamemode. This should add your role to the GameMode if done correctly.