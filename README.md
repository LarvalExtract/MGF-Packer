# MGF-Packer
MGF-Packer is a command-line tool for packaging MGF files for both MechAssault games.
The tool works by packaging all folders and files in a specified folder on your PC in to an MGF file, similar to a zip compression tool (7Zip, WinRAR).

## Usage
To use this tool, you must specify which folder on your PC you want to package, which game the MGF file should be created for, and optionally a name for the MGF file.
Simply run `MGFPacker.exe` without any arguments for help.

### Pack MGF file for MechAssault 1 
```
MGFPacker.exe ma1 C:\folder\to\package
```

### Pack MGF file for MechAssault 2: Lone Wolf and name it MyFile.mgf
```
MGFPacker.exe ma2 C:\folder\to\package -n MyFile
```

The MGF file will be stored inside the folder you specified to package.