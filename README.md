# FileSorter

A Windows desktop application that automatically organizes media files (TV shows, series, etc.) into appropriate folders based on their filename patterns.

## Features

### File Organization
- **Automatic File Organization**: Moves files matching the pattern `xxx.xxx.xxx.S0...*` into folders named `xxx xxx xxx`
- **Smart Folder Creation**: Automatically creates folders if they don't exist
- **Preview Mode**: Preview what changes will be made before executing them

### Folder Merging
- **Smart Folder Merging**: Automatically detects and merges similar folders
- **Intelligent Matching**: Handles variations like:
  - Folders with/without years (e.g., "Show Name 2023" + "Show Name")
  - Different spacing/punctuation (e.g., "Show.Name" + "Show Name")
  - Similar variations (e.g., "Show x x" + "Show xx")
- **Safe Merging**: Preview merges before executing, with confirmation dialogs
- **Duplicate Protection**: Won't overwrite existing files during merge

### File Renaming
- **Clean File Names**: Automatically cleans up messy file names in all subdirectories
- **Removes Clutter**: Removes years, extra punctuation, and text after episode numbers
- **Consistent Format**: Creates clean names like "Show Name S01E01.mkv"
- **Bulk Processing**: Processes all files in all folders at once
- **Safe Renaming**: Preview changes before executing, skips files that already exist

### General
- **User-Friendly Interface**: Clean, modern WPF interface with activity logging
- **Safe Operations**: Confirmation dialogs and duplicate file handling
- **Real-time Logging**: See exactly what's happening as operations proceed

## File Pattern

The application recognizes files in the format:
- **File Format**: `Show.Name.Here.S01E01.mkv` (dots separating words, season indicator)
- **Folder Format**: `Show Name Here` (spaces separating words)

### Examples

| Original File | Destination Folder |
|---------------|-------------------|
| `Breaking.Bad.S01E01.mkv` | `Breaking Bad/` |
| `The.Office.S03E12.mp4` | `The Office/` |
| `Game.Of.Thrones.S05E09.avi` | `Game Of Thrones/` |

## Requirements

- Windows OS
- .NET 8.0 Runtime or SDK

## Installation on Windows

### Option 1: Download Pre-built Release (Easiest)

1. Go to the [Releases](../../releases) page
2. Download the latest `FileSorter.exe`
3. Run the executable directly - no installation required!

### Option 2: Build from Source

#### Prerequisites

1. **Install .NET 8.0 SDK for Windows**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "Windows" and download the SDK installer
   - Run the installer and follow the prompts
   - Verify installation by opening Command Prompt and typing: `dotnet --version`

2. **Install Git for Windows** (if you don't have it)
   - Download from: https://git-scm.com/download/win
   - Run the installer with default settings

#### Build Instructions

1. **Clone the repository:**
   - Open Command Prompt or PowerShell
   - Navigate to where you want to save the project:
     ```cmd
     cd C:\Users\YourName\Documents
     ```
   - Clone the repository:
     ```cmd
     git clone https://github.com/madcrx/FileSorter.git
     cd FileSorter
     ```

2. **Build the application:**
   ```cmd
   dotnet build -c Release
   ```

3. **Run the application:**
   - Option A - Run directly:
     ```cmd
     dotnet run
     ```

   - Option B - Run the executable:
     ```cmd
     bin\Release\net8.0-windows\FileSorter.exe
     ```

4. **Create a desktop shortcut (Optional):**
   - Navigate to `bin\Release\net8.0-windows\` in File Explorer
   - Right-click `FileSorter.exe`
   - Select "Send to" → "Desktop (create shortcut)"

### Option 3: Create Standalone Executable (No .NET Required)

If you want to share the app with others who don't have .NET installed:

1. **Build a self-contained executable:**
   ```cmd
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```

2. **Find the standalone executable:**
   - Location: `bin\Release\net8.0-windows\win-x64\publish\FileSorter.exe`
   - This single file can run on any Windows 10/11 PC without .NET installed
   - File size will be larger (~70-80 MB) but includes everything needed

3. **Distribute:**
   - Copy `FileSorter.exe` from the publish folder
   - Share with others or place on any Windows PC

## Usage

### Organizing Files

1. **Launch the Application**: Run `FileSorter.exe`

2. **Select Directory**:
   - Click the "Browse..." button
   - Navigate to the directory containing your files
   - Click "Select Folder"

3. **Preview Changes** (Optional):
   - Click "Preview Changes" to see what will happen
   - Review the log to ensure files will be organized correctly

4. **Organize Files**:
   - Click "Organize Files"
   - Confirm the operation when prompted
   - Watch the activity log as files are moved

5. **Review Results**:
   - Check the activity log for success/error messages
   - Files are now organized in their respective folders

### Merging Similar Folders

After organizing files, you may end up with similar folders that should be merged:

1. **Select Directory**: Use the same directory selection

2. **Preview Merges** (Recommended):
   - Click "Preview Merges" to see what folders will be combined
   - Review the merge suggestions and reasons
   - Check which files will be moved

3. **Merge Folders**:
   - Click "Merge Similar Folders"
   - Review the confirmation dialog
   - Confirm to proceed with merging
   - Watch the activity log as folders are merged

4. **Review Results**:
   - Similar folders are now combined
   - All files are preserved in the target folder
   - Empty source folders are removed

### Cleaning File Names

After organizing and merging, you can clean up file names to remove clutter:

1. **Select Directory**: Use the same directory selection

2. **Preview Cleaning** (Recommended):
   - Click "Preview Cleaning" to see how files will be renamed
   - Review the before/after file names
   - Check all folders to ensure names look correct

3. **Clean File Names**:
   - Click "Clean File Names"
   - Review the confirmation dialog
   - Confirm to proceed with renaming
   - Watch the activity log as files are renamed

4. **Review Results**:
   - Files now have clean, simple names
   - Format: "Show Name S01E01.ext"
   - All clutter (years, extra text, punctuation) removed

## How It Works

### File Organization

1. **Pattern Matching**: The app scans files in the selected directory looking for the pattern `xxx.xxx.xxx.S##...`

2. **Folder Name Extraction**: It extracts everything before `.S##` and converts dots to spaces

3. **Folder Creation**: If a matching folder doesn't exist, it's created automatically

4. **File Moving**: Files are moved to their corresponding folders

5. **Conflict Handling**: If a file already exists at the destination, it's skipped

### Folder Merging

1. **Similarity Detection**: The app compares folder names using multiple algorithms:
   - Year detection (removes years to compare base names)
   - Normalization (handles spaces, dots, dashes)
   - Fuzzy matching (85% similarity threshold using Levenshtein distance)

2. **Target Selection**: Determines which folder should be the merge target:
   - Prefers folders with years (more specific)
   - Prefers longer, more complete names

3. **Safe Merging**:
   - Moves all files from source to target folder
   - Handles subdirectories
   - Skips duplicate files (won't overwrite)
   - Deletes empty source folders

### Folder Merging Examples

| Before | After | Reason |
|--------|-------|--------|
| `Show Name 2023`<br>`Show Name` | `Show Name 2023` | Same name with/without year |
| `Show.Name`<br>`Show Name` | `Show Name` | Different punctuation |
| `The Office`<br>`The.Office` | `The Office` | Similar base names |
| `Breaking Bad`<br>`Breaking.Bad.2023` | `Breaking.Bad.2023` | Year preference |

### File Renaming

1. **Scans All Folders**: The app looks through all subdirectories in the selected directory

2. **Pattern Detection**: Finds files with season/episode patterns (S01E01, s01e01, etc.)

3. **Name Cleaning**:
   - Removes 4-digit years (1900-2099)
   - Converts dots, dashes, underscores to spaces
   - Removes all text after the episode number
   - Removes multiple spaces
   - Trims whitespace

4. **Fallback Handling**: If the cleaned name is too short or empty, uses the folder name

5. **Clean Format**: Creates files in format: "Show Name S01E01.ext"

### File Renaming Examples

| Before | After |
|--------|-------|
| `Show.Name.2025.S01E01.WEB.x264.mkv` | `Show Name S01E01.mkv` |
| `The.Office.s03e12.HDTV.XviD-LOL.avi` | `The Office s03e12.avi` |
| `Breaking_Bad_2008_S05E16_720p.mp4` | `Breaking Bad S05E16.mp4` |
| `Game.of.Thrones.S08E06.1080p.AMZN.WEB-DL.mkv` | `Game of Thrones S08E06.mkv` |

## Interface Guide

### File Organization Section
- **Directory Selection**: Text box and browse button to select the target directory
- **Organize Files**: Execute the file organization (shows confirmation dialog)
- **Preview Changes**: See what file moves will happen without making changes

### Folder Merging Section
- **Merge Similar Folders**: Execute folder merging (shows confirmation dialog)
- **Preview Merges**: See what folders will be merged without making changes

### File Renaming Section
- **Clean File Names**: Execute file name cleaning (shows confirmation dialog)
- **Preview Cleaning**: See how files will be renamed without making changes

### General
- **Clear Log**: Clear the activity log
- **Activity Log**: Shows real-time progress and results
- **Status Bar**: Displays current operation status

## Safety Features

- **Confirmation Dialog**: Asks for confirmation before moving files
- **Duplicate Detection**: Won't overwrite existing files
- **Error Handling**: Logs errors without crashing
- **Preview Mode**: Test before executing

## Troubleshooting

### Application Won't Start

**"The application requires .NET 8.0"**
- Download and install .NET 8.0 Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
- Choose "Download .NET Desktop Runtime" for Windows
- Restart your computer after installation

**Windows SmartScreen Warning**
- Click "More info" → "Run anyway"
- This happens because the app isn't signed with a commercial certificate
- The application is safe and open-source

### No Files Found

- **File pattern**: Ensure files follow the pattern `xxx.xxx.xxx.S##...`
  - Example: `Show.Name.S01E01.mkv` ✓
  - Example: `ShowNameS01E01.mkv` ✗ (no dots)
- **Season indicator**: Files must have `.S01`, `.S02`, etc.
- **Location**: Make sure you selected the correct folder containing the files
- **File extensions**: Works with any extension (.mkv, .mp4, .avi, etc.)

### Files Not Moving

**Permission Errors:**
- Run the application as Administrator (right-click → "Run as administrator")
- Check if the folder is read-only (Properties → uncheck "Read-only")
- Make sure you have write permissions to the directory

**Files in Use:**
- Close any media players or applications that might have files open
- Check if files are being downloaded or accessed by another program

**Files Already Exist:**
- The app won't overwrite existing files
- Check the destination folder - files might already be there
- Review the activity log for "SKIP" messages

### Preview Shows Files But Organize Doesn't Work

- Check the activity log for specific error messages
- Verify you have sufficient disk space
- Ensure the file paths aren't too long (Windows has a 260 character limit)

### Special Characters in Filenames

- Files with special characters (%, #, etc.) should work fine
- If issues occur, try renaming files to remove unusual characters
- The app handles spaces, periods, and standard characters

## Quick Start Guide (Windows)

**For first-time users:**

1. Download and install .NET 8.0 Runtime from Microsoft
2. Download `FileSorter.exe` or build from source
3. Double-click `FileSorter.exe` to launch
4. Click "Browse..." and select your downloads folder
5. Click "Preview Changes" to see what will happen
6. Click "Organize Files" to sort your media files
7. (Optional) Click "Preview Merges" to check for similar folders
8. (Optional) Click "Merge Similar Folders" to combine duplicate folders
9. (Optional) Click "Preview Cleaning" to see file name changes
10. (Optional) Click "Clean File Names" to remove clutter from file names
11. Done! Your files are now perfectly organized

### Typical Workflow

1. **Step 1 - Organize**: Organize files into folders based on show names
2. **Step 2 - Merge**: Merge any similar folders that were created (e.g., "Show Name" and "Show Name 2023")
3. **Step 3 - Clean**: Clean file names to remove years, extra text, and punctuation
4. **Result**: Clean, organized directory with consistent file names and no duplicate folders

### Example Complete Workflow

**Before:**
```
Downloads/
├── Show.Name.2025.S01E01.WEB.x264.mkv
├── Show.Name.2025.S01E02.720p.HDTV.mkv
├── Show-Name-S01E03-1080p.mkv
```

**After Step 1 (Organize):**
```
Downloads/
├── Show Name/
│   ├── Show.Name.2025.S01E01.WEB.x264.mkv
│   ├── Show.Name.2025.S01E02.720p.HDTV.mkv
│   └── Show-Name-S01E03-1080p.mkv
```

**After Step 2 (Merge):**
```
Downloads/
├── Show Name/
│   ├── Show.Name.2025.S01E01.WEB.x264.mkv
│   ├── Show.Name.2025.S01E02.720p.HDTV.mkv
│   └── Show-Name-S01E03-1080p.mkv
```

**After Step 3 (Clean):**
```
Downloads/
├── Show Name/
│   ├── Show Name S01E01.mkv
│   ├── Show Name S01E02.mkv
│   └── Show Name S01E03.mkv
```

## License

This project is open source and available for personal and educational use.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
