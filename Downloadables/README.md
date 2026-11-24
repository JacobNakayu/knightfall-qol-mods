# Available Mods
## Dawn Team Respawn (REQUIRED)
Allows players to revive under the following conditions:
1. Your entire team is dead
2. Your horse is in a town
3. It is morning
4. You are within 8 m of your horse

Includes text banners and notifications regarding revive events.

> [!WARNING]
> If anybody at all in your lobby has this mod, everybody needs the most up-to-date verison of the mod.
>
> This mod adds fully new game mechanics and patches behavior in ways that require all games to be in sync.

## Enhanced Inventory
Highlights your currently held item on your hotbar.

Additionally, allows you to configure a "scrolling range" so you can use your mouse wheel to navigate your inventory.

Settings for the scrolling range can be found in your local game directory under `KnightFall/UserData/EnhancedInventory.cfg` after first load with the mod.

## In-Town HUD
Provides a simple icon and message displaying when you are considered "in town" and which town you are in.

## Singleton Teams
Unmodded, the game automatically fills teams of 2 no matter the player count, so a lobby of 3 players would have a game with a 2-person team and a 1-person team.

This provides a way for the host to configure the minimum number of players that must be in a lobby before 2-player teams are formed, and automatically prevents 2-person teams from being formed in odd-populated lobbies below 15 players.

Settings for the scrolling range can be found in your local game directory under `KnightFall/UserData/SingletonTeams.cfg` after first load with the mod.

## Town Boundary Force Field
Makes town borders visible by covering them in a sort of sparkly "force field" so that you can clearly see when you are in or out of town.



# Getting Started with Mods
All mods require MelonLoader and the .Net framework to function.

Once you run the game for the first time with MelonLoader installed, activating a mod is as simple as downloading one of the `.dll` files from this page and putting it in your game's new `Mods/` folder.

You can find your game directory by going to the Steam page and navigating to `Settings > Manage > Browse Local Files`.

## Windows
### .Net
Go to [Microsoft's .Net Installation Guide](https://learn.microsoft.com/en-us/dotnet/core/install/windows) and follow the installation instructions.

You should be fine to pick either `.NET Desktop Runtime` if you just want to play, but you'll need the full sdk if you want to develop mods.

###  MelonLoader
Go to [MelonLoader's Home Site](https://melon-loader.com/#getting-started) and follow the install instructions. These should include downloading MelonLoader, unzipping it in your game's root directory, and running the game. It is likely that if .NET is not installed, or not installed correctly, you will be prompted to download it in this step. An indication that MelonLoader has been installed correctly is a terminal that will open when you launch the game. 

After downloading the `.dll` files in this folder that contain your desired mods and placing them in the `/Mods` as directed in the MelonLoader instructions, run the game again. This time, read the console and verify the files are what you expect. You should see a series of lines that look something like
```
[11:09:33.120] ------------------------------
[11:09:33.121] Enhanced Inventory v0.1.0
[11:09:33.121] by Jacob Nakayu
[11:09:33.122] Assembly: EnhancedInventory.dll
[11:09:33.123] ------------------------------
```
for each mod you load. These indicate mods are detected. Near the bottom of the console a small report should load that reads `[11:26:41.200] 5 Mods loaded. `. Make sure this matches the number of downloads you placed in the file. It's not uncommon for mods to be detected but unable to load for a number of reasons. Similarly, if the number of loaded mods greatly exceeds your expected count, it's possible you are running malware that you don't need. This should not be a concern for the mods in this repository.

### Smart App Control
On windows, there are many security features intended to prevent less technical users from running malware, but some of these features can block untrusted files such as community created mods from running. While the specific error message and fix might vary from user to user, a common source of these blocks is **Windows 11 Smart App Control**. If your mods are being detected but an error message says `An Application Control Policy has blocked this file`, consider turning off Smart App Control. 

> [!WARNING]
> Smart App Control prevents untrusted or unsigned files from running on your computer. It is intended to prevent ransomware and other malware from running without your permission.
>
> Smart App Control is not always helpful and can prevent trusted files from running, but having it generally makes your device more secure and it should only be disabled if you trust the files you are running. Consider enabling it again after finishing your play session. 

Smart App Control can be disabled by opening 

**Windows Security &rarr; App & Browser Control &rarr; Smart App Control** 

and checking the box from on to off. Because this does decrease your security it is recommended to re-enable it after you are done playing which can be done from the same menu.

## Linux
Download the [MelonLoader Linux Installer](https://github.com/LavaGang/MelonLoader.Installer/releases/latest/download/MelonLoader.Installer.Linux) from that GitHub link.

Select `Version 0.7.3` and **do not** enable nightly builds.

Point the installer at your game directory from Steam.

Force the use of Proton for the game by going to `Settings > Properties > Compatibility` on the Steam library page and selecting `Proton Experimental`.

In that same window, under `General`, add the line `WINEDLLOVERRIDES="version=n,b" %command%` to the Launch Options input.
