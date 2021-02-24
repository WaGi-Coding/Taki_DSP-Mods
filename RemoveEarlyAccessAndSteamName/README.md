# Free Foundations - Free Soil - Higher Build Range
## Features
With this Mod, you do not need to have any Soil(optional) or Foundations(optional) to actually build foundations. However, you need to have the Foundations unlocked already. Also optional, Higher Build Range. On top of that, the Reform-Size increases from 10x10 to 30x30.

Optionally you can disable Higher Buildrange functionality & you can also set the buildrange yourself. The default value of 250 is the maximum already though.

Checkout the config of this mod! Also make sure to use GodMode-Build-Mode (in the Game settings)

### Configurable Settings
Setting                         |Possible Values      |Default    |Description                                              |Vanilla-Value
-                               |-                    |-          |-                                                        |-
`Enable Free Foundations`       |True/False           |True       |If enabled, you do not need Foundations to build them.   |False
`Enable Free Soil`              |True/False           |True       |If enabled, you do not need Soil to build Foundations.   |False
`Enable Soil Collect`           |True/False           |True       |If enabled, you will collect Soil as usual.              |True
`Enable Higher Buildrange`      |True/False           |True       |Enables/Disabled Higher Build-Range modification.        |None
`Build Range`                   |80-250               |250        |Will set the Build Range.                                |80

### Information
`You may notice weird behavior of building when using multiple mods altering the DetermineBuildPreviews Method in Foundation Building Mode.`
30x30 Reform-Size is incompatible with `BuildAnywhere` by Alejandro and does get hard locked to 10x10!

## Installation
### With Mod Manager

Simply open the mod manager (if you don't have it install it [here](https://dsp.thunderstore.io/package/ebkr/r2modman/)), select **FreeFoundations_FreeSoil_HigherBuildRange by Taki7o7**, then **Download**. 

If prompted to download with dependencies, select Yes.

Then just click **Start modded**, and the game will run with the mod installed.

### Manually
Install BepInEx [here](https://dsp.thunderstore.io/package/xiaoye97/BepInEx/)

Then drag FreeFoundationsFreeSoil.dll into steamapps/common/Dyson Sphere Program/BepInEx/plugins

## Feedback / Bug Reports
Feel free to contact me via Discord (Taki7o7#1753) for any feedback, bug-reports or suggestions

## Changelog
### v1.1.9
- Added ingame info, which will show you if FreeFoundations and/or FreeSoil is enabled
- If running BuildAnywhere Mod by Alejandro, log a warning that 30x30 Reform-Size is not working with it will stay 10x10
### v1.1.8
- Max. Reformsize is now 30x30 instead of 10x10
### v1.1.7
- updated readme (replaced r2modman link)
### v1.1.6
- return normal build determine action if not in Foundation-Building mode (This should hopefully fix most problems with other mods)
- entirely disable custom code execution for building if FreeSoil & FreeFoundations settings are both disabled.
### v1.1.5
- fixed some control issues
- adjusted readme (ye again)
### v1.1.1 - v1.1.4
- changed Readme
### v1.1.0
- added option to Enable/Disable Soil-Collecting
- made everything optional
- displaying amount of actual needed Soil when determine build preview
### v1.0.0
- Initial release