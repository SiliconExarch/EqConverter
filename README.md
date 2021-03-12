# EqConverter

EqConverter is a simple command-line C# application to convert AutoEq ParametricEQ.txt files into USB Audio Player Pro ToneBoosters PEQ xml presets.

## Installation

Extract the folder from the release archive.

## Usage

```
EqConverter.exe [-fv] file1 file2 file3...

OPTIONS
-f  Use containing folders as preset names, as opposed to using the name of the original preset file. 
    Useful if you want to convert a bunch of different sound signatures for the same headphone.

-v  Enable verbose message output
```
Output files are saved in a 'TBEQPresets' folder next to the executable that will be created if it does not exist. This folder can then be copied in its entirety into the UAPP directory on your phone's internal storage.

You can also drag and drop multiple .txt files (for example from a list of search results) directly onto the .exe, or a shortcut to it (in case you want to use the -f option) and they will all be converted.

## Compiling

Open `EqConverter.sln` in Visual Studio 2019. 

Right-click on the Solution and choose **Restore NuGet Packages** before building for the first time.

## Credits
[@Cashback-Git](https://github.com/Cashback-Git) for inspiration and motivation.

[Command Line Parser](https://github.com/commandlineparser/commandline) for a great library.

Davy Wentzler from [eXtreamSD](https://www.extreamsd.com/) for the excellent USB Audio Player Pro and help with Freq and Q calculation.

## Contributing
Pull requests are welcome. 

## License
[Apache 2.0](https://choosealicense.com/licenses/apache-2.0/)
