﻿# ErogePlugins

Various plugins and fixes, for various Unity-based eroges.

## Plugins within this project

### GetTapped

Adds mouse-only features (e.g. camera rotation) to tablet users. May also contain fixes.

| Game     | Project | Status |
|----------|---------|---------|
| (Plugin core)|GetTapped.Core|✅
| KoiKatsu | GetTapped.KK| ✅
| Insult Order | GetTapped.IO| ❔
| COM3D2 | GetTapped.com3d2 | ❔

### FixKPlug

Performance fix on H-Scene loading for KPlug (KoiKatsu) users

| Game | Project | Status |
|----|----|----|
|KoiKatsu|FixKPlug|✅

### FixEyeMov

Generate fixational eye movement for characters to make them more life-like.

| Game | Project | Status |
|----|----|----|
|(Plugin core)|FixEyeMov.Core |✅
|COM3D2 |FixEyeMov.com3d2|✅

### SocialForces

Implements the [Social Forces model][socialforces] (Dirk Helbing, et al. 1998) in Koikatsu.

I have ABSOLUTELY no idea why I'm doing this. It's buggy, and the numbers need a further refinement.

## Installing

**Requires:**

- BepInEx > 5.3

Steps:

1. Copy `BepInEx` into game folder, overwrite files if necessary
2. Run game
3. ???
4. Success!


## Building

1. Place the corresponding files in `refrence/` (see below)
2. Open project in Visual Studio.
2. Hit "Build" really hard.
3. Your output should be located in `Release/<project-name>`.
4. Install and enjoy!

### Game files

**For all plugins and games:** Copy the `Assembly-CSharp.dll` of your designated game to `reference/<your-game-name>/`.

|Game|folder|
|----|------|
|COM3D2 | `com3d2` |
| Insult Order | `insultorder` |
| KoiKatsu | `koikatsu`|
| KoiKatsu Sunshine | `koikatsu-sunshine`|


#### Extra files for games / plugins

**FixKPlug:** 

- In `reference/games/koikatsu/`:
  - Copy `kPlug.dll` and `ExtensibleSaveFormat.dll`

**KoiKatsu Sunshine:**

- In `reference/games/koikatsu-sunshine/`:
  - Copy `UnityEngine.dll` and `UnityEngine.{CoreModule,InputLegacyModule,UI,UIModule}.dll`
  - Add `.kkss` before `.dll` in files above

## License

All plugin mods are licenced under [the MIT license](https://opensource.org/licenses/MIT).

Any other resource (e.g. models, textures, etc.) are licensed under [CC BY-SA 4.0 License](https://creativecommons.org/licenses/by-sa/4.0/).

Copyright (c) 2019-2020 Karenia Works.
