# SZ Extractor

A basic command-line tool for extracting **known (raw) files** and folders from Unreal Engine game data archives, specifically for **Dragon Ball: Sparking! Zero** but may work for many other games still. Based on [FModel](https://github.com/4sval/FModel/tree/master) and [CUE4Parse](https://github.com/FabianFG/CUE4Parse/tree/master).

## Usage

```bash
SZ_Extractor -p <content-path> -e <engine-version> -k <aes-key> -g <game-dir> [-o <output-dir>] [-d] [-v]
```

### Arguments

*   `-p`, `--content-path`: **(Required)** Virtual path to extract (e.g., `SparkingZERO/Content/CriWareData/bgm_main.awb`).
*   `-e`, `--engine-version`: **(Required)** Unreal Engine version (e.g., `GAME_UE5_1`).
*   `-k`, `--aes-key`: **(Required)** AES key in hex format (e.g., `0x1234...`).
*   `-g`, `--game-dir`: **(Required)** Path to the game's `\Paks` directory.
*   `-o`, `--output`: (Optional) Output directory. Defaults to `Output`.
*   `-d`, `--dump-paths`: (Optional) Dump all virtual file paths to `paths.json`. Very needed.
*   `-v`, `--verbose`: (Optional) Enable verbose logging.

### Example

```bash
SZ_Extractor -p Game/Characters/Goku/Costumes/Base/Goku_Base.uasset -e GAME_UE5_1 -k 0xYourAesKeyHere -g "C:\Games\Dragon Ball Example\Paks"
```

## Notes

*   Case-Insensitive for virtual paths. 
*   You must know the paths beforehand. This recursively extracts subfolders so you can still manage without knowing the full path. Use Fmodel or the dump argument to see everything.
*   Does not perform any conversion, raw data as it's found.

## Disclaimer

Mainly for Sparking Zero, Fmodel is a lot more robust and offers conversion features along with many other things, this is mainly for devs to use since I haven't seen a cli version of Fmodel

## TODOs
For reference:
- Duplicate handling
- Pakchunk file attribution (in order to separate game files from mod files mostly)
- Direct file search (without having the full path), easy implementation
- Bulk fetch (which requires multiple startups normally), or a continuous open stream
