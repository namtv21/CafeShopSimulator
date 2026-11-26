# CafeShopSimulator

A PC cafe management prototype built with Unity. Gameplay includes drink preparation, customer orders, staff service, shop upgrades, and a town with pedestrian and vehicle activity.

## Requirements

- Unity 6000.3.28f1
- Local artwork and character assets listed in `local-assets.txt`

## Setup

This repository contains source code, scenes, and project configuration. Artwork, character assets, fonts, audio, and builds are not included.

Restore the required local assets before opening Unity:

```powershell
.\Restore-LocalAssets.ps1 -LocalAssetFolder 'C:/path/to/local-assets'
```

The local asset directory must preserve the `Assets/` paths and corresponding `.meta` files listed in the manifest so scene references resolve correctly.

Open `Assets/Scenes/MainMenu.unity` for the normal game flow. Open `Town.unity` or `Gameplay.unity` directly to test a temporary session without modifying a saved game.

## Project structure

- `Assets/Scripts/Core`: gameplay rules and state
- `Assets/Scripts/Flow`: session and scene transitions
- `Assets/Scripts/UI`: interface bindings and scene behavior
- `Assets/Scenes`: game scenes
- `ProjectSettings` and `Packages`: Unity configuration
