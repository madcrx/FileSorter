# FileSorter

A Windows desktop application that automatically organizes media files (TV shows, series, etc.) into appropriate folders based on their filename patterns.

## Features

- **Automatic File Organization**: Moves files matching the pattern `xxx.xxx.xxx.S0...*` into folders named `xxx xxx xxx`
- **Smart Folder Creation**: Automatically creates folders if they don't exist
- **Preview Mode**: Preview what changes will be made before executing them
- **User-Friendly Interface**: Clean, modern WPF interface with activity logging
- **Safe Operations**: Confirmation dialogs and duplicate file handling

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

## Building the Application

### Prerequisites

Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Instructions

1. Clone this repository:
   ```bash
   git clone <repository-url>
   cd FileSorter
   ```

2. Build the project:
   ```bash
   dotnet build -c Release
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

   Or find the executable at `bin/Release/net8.0-windows/FileSorter.exe`

## Usage

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

## How It Works

1. **Pattern Matching**: The app scans files in the selected directory looking for the pattern `xxx.xxx.xxx.S##...`

2. **Folder Name Extraction**: It extracts everything before `.S##` and converts dots to spaces

3. **Folder Creation**: If a matching folder doesn't exist, it's created automatically

4. **File Moving**: Files are moved to their corresponding folders

5. **Conflict Handling**: If a file already exists at the destination, it's skipped

## Interface Guide

- **Directory Selection**: Text box and browse button to select the target directory
- **Organize Files**: Execute the file organization (shows confirmation dialog)
- **Preview Changes**: See what will happen without making changes
- **Clear Log**: Clear the activity log
- **Activity Log**: Shows real-time progress and results
- **Status Bar**: Displays current operation status

## Safety Features

- **Confirmation Dialog**: Asks for confirmation before moving files
- **Duplicate Detection**: Won't overwrite existing files
- **Error Handling**: Logs errors without crashing
- **Preview Mode**: Test before executing

## Troubleshooting

### No Files Found
- Ensure files follow the pattern: `xxx.xxx.xxx.S##...`
- Check that files have a season indicator (S01, S02, etc.)
- Verify you selected the correct directory

### Files Not Moving
- Check file permissions
- Ensure the directory is not read-only
- Verify no other application has files open

### Application Won't Start
- Install .NET 8.0 Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
- Check Windows compatibility

## License

This project is open source and available for personal and educational use.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
