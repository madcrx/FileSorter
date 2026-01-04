# FileSorter

A Windows desktop application that automatically organizes media files (TV shows, series, etc.) into appropriate folders based on their filename patterns.

## Features

### Interface
- **Tabbed Interface**: Separate tabs for TV Shows and Movies for organized workflow
- **User-Friendly Design**: Clean, modern WPF interface with activity logging
- **Real-time Logging**: See exactly what's happening as operations proceed
- **Safe Operations**: Confirmation dialogs and duplicate file handling

### TV Shows Tab

#### File Organization
- **Automatic File Organization**: Moves files matching the pattern `xxx.xxx.xxx.S0...*` into folders named `xxx xxx xxx`
- **Smart Folder Creation**: Automatically creates folders if they don't exist
- **Preview Mode**: Preview what changes will be made before executing them

#### Folder Merging
- **Smart Folder Merging**: Automatically detects and merges similar folders
- **Intelligent Matching**: Handles variations like:
  - Folders with/without years (e.g., "Show Name 2023" + "Show Name")
  - Different spacing/punctuation (e.g., "Show.Name" + "Show Name")
  - Similar variations (e.g., "Show x x" + "Show xx")
- **Safe Merging**: Preview merges before executing, with confirmation dialogs
- **Duplicate Protection**: Won't overwrite existing files during merge

#### File Renaming
- **Clean File Names**: Automatically cleans up messy file names in all subdirectories
- **Removes Clutter**: Removes years, extra punctuation, and text after episode numbers
- **Consistent Format**: Creates clean names like "Show Name S01E01.mkv"
- **Bulk Processing**: Processes all files in all folders at once
- **Safe Renaming**: Preview changes before executing, skips files that already exist

#### Season Folder Organization
- **Automatic Season Folders**: Organizes files into Season folders within each show directory
- **Smart Folder Creation**: Creates "Season 1", "Season 2", etc. folders automatically
- **Leading Zero Removal**: Renames "Season 01" to "Season 1" for consistency
- **Episode Detection**: Extracts season number from S01E01, S02E03, etc.
- **Bulk Organization**: Processes all shows and all seasons at once
- **Safe Operations**: Preview mode and duplicate detection

### Movies Tab

#### Movie File Cleaning
- **Automatic Movie Cleanup**: Simplifies messy movie file names
- **Year Preservation**: Keeps the movie year while removing clutter
- **Removes Extra Text**: Removes resolution, release info, and other extra text after the year
- **Clean Format**: Creates files in format "Movie Title YEAR.ext"
- **Preview Mode**: See changes before executing
- **Safe Operations**: Preview mode and duplicate detection

## File Patterns

### TV Shows
The application recognizes TV show files in the format:
- **File Format**: `Show.Name.Here.S01E01.mkv` (dots separating words, season indicator)
- **Folder Format**: `Show Name Here` (spaces separating words)

#### TV Show Examples

| Original File | Destination Folder |
|---------------|-------------------|
| `Breaking.Bad.S01E01.mkv` | `Breaking Bad/` |
| `The.Office.S03E12.mp4` | `The Office/` |
| `Game.Of.Thrones.S05E09.avi` | `Game Of Thrones/` |

### Movies
The application recognizes movie files with years and cleans them:
- **Input Format**: `Movie.Title.2021.1080p.WEB.mkv` (dots separating words, year, extra info)
- **Output Format**: `Movie Title 2021.mkv` (clean title with year)

#### Movie Cleaning Examples

| Before Cleaning | After Cleaning |
|-----------------|----------------|
| `The.Matrix.1999.1080p.BluRay.x264.mkv` | `The Matrix 1999.mkv` |
| `Inception.2010.720p.WEB-DL.AAC2.0.H.264.mp4` | `Inception 2010.mp4` |
| `Interstellar-2014-2160p-4K-HDR.mkv` | `Interstellar 2014.mkv` |
| `Avatar_The_Way_of_Water_2022_IMAX.avi` | `Avatar The Way of Water 2022.avi` |

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

### Organizing into Season Folders

After cleaning file names, you can organize files into season-specific subfolders:

1. **Select Directory**: Use the same directory selection

2. **Preview Seasons** (Recommended):
   - Click "Preview Seasons" to see how files will be organized
   - Review which files go into which season folders
   - Check if season folders will be created or renamed

3. **Organize into Seasons**:
   - Click "Organize into Seasons"
   - Review the confirmation dialog
   - Confirm to proceed with organization
   - Watch the activity log as files are moved

4. **Review Results**:
   - Files are now organized in Season folders
   - Format: "Show Name/Season 1/Show Name S01E01.mkv"
   - Leading zeros removed from folder names (Season 01 → Season 1)

### Cleaning Movie Files

For movie files, switch to the Movies tab to clean up file names:

1. **Switch to Movies Tab**: Click the "Movies" tab at the top

2. **Select Directory**: Use the directory selection to choose the folder with movie files

3. **Preview Cleaning** (Recommended):
   - Click "Preview Cleaning" to see how files will be renamed
   - Review the before/after file names
   - Verify the year is preserved and extra text is removed

4. **Clean Movie Files**:
   - Click "Clean Movie Files"
   - Review the confirmation dialog
   - Confirm to proceed with renaming
   - Watch the activity log as files are renamed

5. **Review Results**:
   - Files now have clean names with the year preserved
   - Format: "Movie Title YEAR.ext"
   - All extra text (resolution, release info, etc.) removed

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

### Season Folder Organization

1. **Scans Show Folders**: The app looks through all subdirectories (show folders)

2. **Season Detection**: Extracts season number from file names (S01, S02, S03, etc.)

3. **Folder Naming**: Creates folders without leading zeros:
   - S01E01 → "Season 1"
   - S02E03 → "Season 2"
   - S12E05 → "Season 12"

4. **Leading Zero Removal**: Automatically renames existing folders:
   - "Season 01" → "Season 1"
   - "Season 02" → "Season 2"

5. **File Organization**: Moves files to their corresponding season folder

### Season Folder Examples

**Before Organization:**
```
Show Name/
├── Show Name S01E01.mkv
├── Show Name S01E02.mkv
├── Show Name S02E01.mkv
├── Show Name S02E02.mkv
```

**After Organization:**
```
Show Name/
├── Season 1/
│   ├── Show Name S01E01.mkv
│   └── Show Name S01E02.mkv
└── Season 2/
    ├── Show Name S02E01.mkv
    └── Show Name S02E02.mkv
```

## Interface Guide

### Main Interface
- **Directory Selection**: Text box and browse button to select the target directory (shared between tabs)
- **TV Shows Tab**: Contains all TV show organization features
- **Movies Tab**: Contains movie file cleaning features
- **Status Bar**: Displays current operation status (shared between tabs)
- **Clear Log**: Clears the activity log in the current tab

### TV Shows Tab

#### File Organization Section
- **Organize Files**: Execute the file organization (shows confirmation dialog)
- **Preview Changes**: See what file moves will happen without making changes

#### Folder Merging Section
- **Merge Similar Folders**: Execute folder merging (shows confirmation dialog)
- **Preview Merges**: See what folders will be merged without making changes

#### File Renaming Section
- **Clean File Names**: Execute file name cleaning (shows confirmation dialog)
- **Preview Cleaning**: See how files will be renamed without making changes

#### Season Folder Organization Section
- **Organize into Seasons**: Execute season folder organization (shows confirmation dialog)
- **Preview Seasons**: See how files will be organized into season folders

#### Activity Log
- Shows real-time progress and results for TV show operations

### Movies Tab

#### Movie File Cleaning Section
- **Clean Movie Files**: Execute movie file name cleaning (shows confirmation dialog)
- **Preview Cleaning**: See how movie files will be renamed without making changes

#### Activity Log
- Shows real-time progress and results for movie operations

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

**For TV Shows (TV Shows tab):**

5. Click "Preview Changes" to see what will happen
6. Click "Organize Files" to sort your TV show files
7. (Optional) Click "Preview Merges" to check for similar folders
8. (Optional) Click "Merge Similar Folders" to combine duplicate folders
9. (Optional) Click "Preview Cleaning" to see file name changes
10. (Optional) Click "Clean File Names" to remove clutter from file names
11. (Optional) Click "Preview Seasons" to see season folder organization
12. (Optional) Click "Organize into Seasons" to sort files into season folders

**For Movies (Movies tab):**

5. Switch to the "Movies" tab
6. Click "Preview Cleaning" to see how movie files will be renamed
7. Click "Clean Movie Files" to simplify movie file names

**Done!** Your files are now perfectly organized

### Typical Workflow

#### TV Shows (TV Shows Tab)
1. **Step 1 - Organize**: Organize files into folders based on show names
2. **Step 2 - Merge**: Merge any similar folders that were created (e.g., "Show Name" and "Show Name 2023")
3. **Step 3 - Clean**: Clean file names to remove years, extra text, and punctuation
4. **Step 4 - Seasons**: Organize files into season folders (Season 1, Season 2, etc.)
5. **Result**: Clean, organized directory with consistent file names, no duplicate folders, and organized seasons

#### Movies (Movies Tab)
1. **Step 1 - Clean**: Clean movie file names to remove resolution, release info, and other clutter
2. **Result**: Clean movie files in format "Movie Title YEAR.ext"

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

**After Step 4 (Organize into Seasons):**
```
Downloads/
├── Show Name/
│   └── Season 1/
│       ├── Show Name S01E01.mkv
│       ├── Show Name S01E02.mkv
│       └── Show Name S01E03.mkv
```

### Movie File Cleaning

1. **Year Detection**: The app searches for a 4-digit year (1900-2099) in the filename

2. **Title Extraction**: Extracts everything before the year as the movie title

3. **Name Cleaning**:
   - Replaces dots, dashes, underscores with spaces
   - Removes multiple spaces
   - Trims whitespace

4. **Format Output**: Creates clean file name: "Movie Title YEAR.ext"

5. **Fallback Handling**: If no year is found, still cleans up punctuation

### Movie Cleaning Examples

| Before | After |
|--------|-------|
| `The.Matrix.1999.1080p.BluRay.x264.mkv` | `The Matrix 1999.mkv` |
| `Inception.2010.720p.WEB-DL.AAC2.0.H.264.mp4` | `Inception 2010.mp4` |
| `Interstellar-2014-2160p-4K-HDR.mkv` | `Interstellar 2014.mkv` |
| `Avatar_The_Way_of_Water_2022_IMAX.avi` | `Avatar The Way of Water 2022.avi` |
| `The_Dark_Knight.mkv` | `The Dark Knight.mkv` (no year found, basic cleanup) |

## License

This project is open source and available for personal and educational use.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
