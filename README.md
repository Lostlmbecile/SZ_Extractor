# SZ Extractor

A basic command-line tool for extracting **known (raw) files** and folders from Unreal Engine game data archives, specifically for **Dragon Ball: Sparking! Zero** but may work for many other games still. Based on [FModel](https://github.com/4sval/FModel/tree/master) and [CUE4Parse](https://github.com/FabianFG/CUE4Parse/tree/master).

If you want a server/api version see [this repo](https://github.com/Lostlmbecile/SZ_Extractor_Server)
## Usage

```bash
SZ_Extractor -p <content-path> -e <engine-version> -k <aes-key> -g <game-dir> [-o <output-dir>] [-d] [-v]
```

### Arguments

*   `-p`, `--content-path`: **(Required)** Virtual path/fileName to extract (e.g., `SparkingZERO/Content/CriWareData/bgm_main.awb` or `bgm_main.awb`).
*   `-e`, `--engine-version`: **(Required)** Unreal Engine version (e.g., `GAME_UE5_1`).
*   `-k`, `--aes-key`: **(Required)** AES key in hex format (e.g., `0x1234...`).
*   `-g`, `--game-dir`: **(Required)** Path to the game's `\Paks` directory.
*   `-o`, `--output`: (Optional) Output directory. Defaults to `Output`.
*   `-d`, `--dump-paths`: (Optional) Dump all virtual file paths to `paths.json`.
*   `-v`, `--verbose`: (Optional) Enable verbose logging.

You don't need to specify `--content-path` if `--dump-paths` is sent.
### Example

```bash
# Full Path to File
SZ_Extractor -p "Game/Characters/Goku/Costumes/Base/Goku_Base.uasset" -e GAME_UE5_1 -k 0xYourAesKeyHere -g "C:\Games\Dragon Ball Example\Paks"
```
```bash
# File Name Only
SZ_Extractor -p "Goku_Base.uasset" -e GAME_UE5_1 -k 0xYourAesKeyHere -g "C:\Games\Dragon Ball Example\Paks"
```
```bash
# Folder Name (All SubFolders are also extracted)
SZ_Extractor -p "Game/Characters/Goku/Costumes" -e GAME_UE5_1 -k 0xYourAesKeyHere -g "C:\Games\Dragon Ball Example\Paks"
```
Note that duplicate files (same file in multiple archives) will be put inside a folder of their archive's name, so no overrides will happen.
## Notes

*   Case-Insensitive for virtual paths. 
*   You must know the paths or file names beforehand. Use Fmodel or the dump argument (-d) to see all, including duplicates.
*   Does not perform any conversion, raw data as it's found.

## Disclaimer

Mainly for Sparking Zero, Fmodel is a lot more robust and offers conversion features along with many other things, this is mainly for devs to use since I haven't seen a cli version of Fmodel
