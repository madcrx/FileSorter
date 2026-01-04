using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Newtonsoft.Json.Linq;
using TagLib;
using File = System.IO.File;

namespace FileSorter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LogMessage("Application started. Select a directory to organize files.");
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the directory containing files to organize";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DirectoryTextBox.Text = dialog.SelectedPath;
                    LogMessage($"Selected directory: {dialog.SelectedPath}");
                }
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMessage("\n=== PREVIEW MODE ===");
            LogMessage("Scanning directory for files to organize...\n");

            try
            {
                var operations = AnalyzeDirectory(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMessage("No files found matching the pattern (xxx.xxx.xxx.S0...)");
                    StatusTextBlock.Text = "No files to organize";
                    return;
                }

                LogMessage($"Found {operations.Count} file(s) to organize:\n");

                foreach (var op in operations)
                {
                    string folderStatus = Directory.Exists(op.DestinationFolder) ? "[EXISTS]" : "[WILL CREATE]";
                    LogMessage($"{folderStatus} {op.FileName}");
                    LogMessage($"  → {op.DestinationFolder}\n");
                }

                StatusTextBlock.Text = $"Preview: {operations.Count} files ready to organize";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void OrganizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var result = MessageBox.Show(
                "This will move files to their organized folders. Continue?",
                "Confirm Organization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMessage("\n=== ORGANIZING FILES ===");
            LogMessage("Starting file organization...\n");

            try
            {
                var operations = AnalyzeDirectory(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMessage("No files found matching the pattern (xxx.xxx.xxx.S0...)");
                    StatusTextBlock.Text = "No files to organize";
                    return;
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (var op in operations)
                {
                    try
                    {
                        // Create folder if it doesn't exist
                        if (!Directory.Exists(op.DestinationFolder))
                        {
                            Directory.CreateDirectory(op.DestinationFolder);
                            LogMessage($"[CREATED] Folder: {Path.GetFileName(op.DestinationFolder)}");
                        }

                        // Move the file
                        string destPath = Path.Combine(op.DestinationFolder, op.FileName);

                        // Handle file already exists at destination
                        if (File.Exists(destPath))
                        {
                            LogMessage($"[SKIP] File already exists: {op.FileName}");
                            continue;
                        }

                        File.Move(op.SourcePath, destPath);
                        LogMessage($"[MOVED] {op.FileName} → {Path.GetFileName(op.DestinationFolder)}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"[ERROR] Failed to move {op.FileName}: {ex.Message}");
                        errorCount++;
                    }
                }

                LogMessage($"\n=== COMPLETE ===");
                LogMessage($"Successfully organized: {successCount} files");
                if (errorCount > 0)
                    LogMessage($"Errors: {errorCount} files");

                StatusTextBlock.Text = $"Organized {successCount} files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"File organization complete!\n\nSuccessfully moved: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during organization";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBlock.Text = string.Empty;
            MovieLogTextBlock.Text = string.Empty;
            MusicLogTextBlock.Text = string.Empty;
            StatusTextBlock.Text = "Ready";
        }

        private void PreviewMovieCleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMovieMessage("\n=== PREVIEW MOVIE FILE CLEANING ===");
            LogMovieMessage("Scanning directory for movie files...\n");

            try
            {
                var renameOperations = AnalyzeMoviesForCleaning(DirectoryTextBox.Text);

                if (renameOperations.Count == 0)
                {
                    LogMovieMessage("No movie files found that need cleaning.");
                    StatusTextBlock.Text = "No movie files to clean";
                    return;
                }

                LogMovieMessage($"Found {renameOperations.Count} movie file(s) to clean:\n");

                foreach (var rename in renameOperations)
                {
                    LogMovieMessage($"[RENAME]");
                    LogMovieMessage($"  FROM: {rename.OldFileName}");
                    LogMovieMessage($"  TO:   {rename.NewFileName}\n");
                }

                StatusTextBlock.Text = $"Preview: {renameOperations.Count} movie files ready to clean";
            }
            catch (Exception ex)
            {
                LogMovieMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void CleanMoviesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var renameOperations = AnalyzeMoviesForCleaning(DirectoryTextBox.Text);

            if (renameOperations.Count == 0)
            {
                MessageBox.Show("No movie files found that need cleaning.", "Nothing to Clean",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will rename {renameOperations.Count} movie file(s) to clean format.\n\n" +
                "File names will be simplified (e.g., 'Movie.2021.1080p.WEB.mkv' → 'Movie 2021.mkv'). Continue?",
                "Confirm Movie File Renaming",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMovieMessage("\n=== CLEANING MOVIE FILES ===");
            LogMovieMessage("Starting movie file rename operation...\n");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var rename in renameOperations)
                {
                    try
                    {
                        string oldPath = Path.Combine(rename.FolderPath, rename.OldFileName);
                        string newPath = Path.Combine(rename.FolderPath, rename.NewFileName);

                        // Check if target file already exists
                        if (File.Exists(newPath))
                        {
                            LogMovieMessage($"[SKIP] {rename.OldFileName}");
                            LogMovieMessage($"  Target file already exists: {rename.NewFileName}\n");
                            continue;
                        }

                        File.Move(oldPath, newPath);
                        LogMovieMessage($"[RENAMED]");
                        LogMovieMessage($"  {rename.OldFileName} → {rename.NewFileName}\n");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMovieMessage($"[ERROR] Failed to rename {rename.OldFileName}: {ex.Message}\n");
                        errorCount++;
                    }
                }

                LogMovieMessage($"=== COMPLETE ===");
                LogMovieMessage($"Successfully renamed: {successCount} file(s)");
                if (errorCount > 0)
                    LogMovieMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Cleaned {successCount} movie files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Movie file cleaning complete!\n\nSuccessfully renamed: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMovieMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during movie file cleaning";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewMergeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMessage("\n=== PREVIEW FOLDER MERGES ===");
            LogMessage("Scanning for similar folders...\n");

            try
            {
                var mergeOperations = FindSimilarFolders(DirectoryTextBox.Text);

                if (mergeOperations.Count == 0)
                {
                    LogMessage("No similar folders found to merge.");
                    StatusTextBlock.Text = "No folders to merge";
                    return;
                }

                LogMessage($"Found {mergeOperations.Count} merge operation(s):\n");

                foreach (var merge in mergeOperations)
                {
                    LogMessage($"[MERGE] {merge.SourceFolder}");
                    LogMessage($"  → INTO → {merge.TargetFolder}");
                    LogMessage($"  Reason: {merge.Reason}");
                    LogMessage($"  Files to move: {merge.FileCount}\n");
                }

                StatusTextBlock.Text = $"Preview: {mergeOperations.Count} merge operations ready";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void MergeFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var mergeOperations = FindSimilarFolders(DirectoryTextBox.Text);

            if (mergeOperations.Count == 0)
            {
                MessageBox.Show("No similar folders found to merge.", "Nothing to Merge",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will merge {mergeOperations.Count} folder(s) into their target folders.\n\n" +
                "All files will be moved and source folders will be deleted. Continue?",
                "Confirm Folder Merge",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMessage("\n=== MERGING FOLDERS ===");
            LogMessage("Starting folder merge operation...\n");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var merge in mergeOperations)
                {
                    try
                    {
                        LogMessage($"[PROCESSING] {merge.SourceFolder} → {merge.TargetFolder}");

                        // Create target folder if it doesn't exist
                        if (!Directory.Exists(merge.TargetFolderPath))
                        {
                            Directory.CreateDirectory(merge.TargetFolderPath);
                            LogMessage($"  [CREATED] Target folder");
                        }

                        // Move all files from source to target
                        var files = Directory.GetFiles(merge.SourceFolderPath);
                        int movedFiles = 0;
                        int skippedFiles = 0;

                        foreach (var file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            string destPath = Path.Combine(merge.TargetFolderPath, fileName);

                            if (File.Exists(destPath))
                            {
                                LogMessage($"  [SKIP] {fileName} (already exists)");
                                skippedFiles++;
                            }
                            else
                            {
                                File.Move(file, destPath);
                                movedFiles++;
                            }
                        }

                        // Move subdirectories if any
                        var subdirs = Directory.GetDirectories(merge.SourceFolderPath);
                        foreach (var subdir in subdirs)
                        {
                            string subdirName = Path.GetFileName(subdir);
                            string destSubdir = Path.Combine(merge.TargetFolderPath, subdirName);

                            if (Directory.Exists(destSubdir))
                            {
                                LogMessage($"  [SKIP] Subdirectory {subdirName} (already exists)");
                            }
                            else
                            {
                                Directory.Move(subdir, destSubdir);
                            }
                        }

                        // Delete source folder if empty
                        if (Directory.GetFileSystemEntries(merge.SourceFolderPath).Length == 0)
                        {
                            Directory.Delete(merge.SourceFolderPath);
                            LogMessage($"  [DELETED] Source folder (empty)");
                        }
                        else
                        {
                            LogMessage($"  [KEPT] Source folder (not empty)");
                        }

                        LogMessage($"  [SUCCESS] Moved {movedFiles} file(s), skipped {skippedFiles}\n");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"  [ERROR] {ex.Message}\n");
                        errorCount++;
                    }
                }

                LogMessage($"=== COMPLETE ===");
                LogMessage($"Successfully merged: {successCount} folder(s)");
                if (errorCount > 0)
                    LogMessage($"Errors: {errorCount} folder(s)");

                StatusTextBlock.Text = $"Merged {successCount} folders" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Folder merge complete!\n\nSuccessfully merged: {successCount} folders\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during merge";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewCleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMessage("\n=== PREVIEW FILE NAME CLEANING ===");
            LogMessage("Scanning files in subdirectories...\n");

            try
            {
                var renameOperations = AnalyzeFilesForCleaning(DirectoryTextBox.Text);

                if (renameOperations.Count == 0)
                {
                    LogMessage("No files found that need cleaning.");
                    StatusTextBlock.Text = "No files to clean";
                    return;
                }

                LogMessage($"Found {renameOperations.Count} file(s) to clean:\n");

                foreach (var rename in renameOperations)
                {
                    LogMessage($"[RENAME] {rename.Folder}");
                    LogMessage($"  FROM: {rename.OldFileName}");
                    LogMessage($"  TO:   {rename.NewFileName}\n");
                }

                StatusTextBlock.Text = $"Preview: {renameOperations.Count} files ready to clean";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void CleanFileNamesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var renameOperations = AnalyzeFilesForCleaning(DirectoryTextBox.Text);

            if (renameOperations.Count == 0)
            {
                MessageBox.Show("No files found that need cleaning.", "Nothing to Clean",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will rename {renameOperations.Count} file(s) to clean format.\n\n" +
                "File names will be simplified (remove extra text, years, punctuation). Continue?",
                "Confirm File Renaming",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMessage("\n=== CLEANING FILE NAMES ===");
            LogMessage("Starting file rename operation...\n");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var rename in renameOperations)
                {
                    try
                    {
                        string oldPath = Path.Combine(rename.FolderPath, rename.OldFileName);
                        string newPath = Path.Combine(rename.FolderPath, rename.NewFileName);

                        // Check if target file already exists
                        if (File.Exists(newPath))
                        {
                            LogMessage($"[SKIP] {rename.Folder}/{rename.OldFileName}");
                            LogMessage($"  Target file already exists: {rename.NewFileName}\n");
                            continue;
                        }

                        File.Move(oldPath, newPath);
                        LogMessage($"[RENAMED] {rename.Folder}");
                        LogMessage($"  {rename.OldFileName} → {rename.NewFileName}\n");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"[ERROR] Failed to rename {rename.OldFileName}: {ex.Message}\n");
                        errorCount++;
                    }
                }

                LogMessage($"=== COMPLETE ===");
                LogMessage($"Successfully renamed: {successCount} file(s)");
                if (errorCount > 0)
                    LogMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Cleaned {successCount} files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"File cleaning complete!\n\nSuccessfully renamed: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during file cleaning";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewSeasonsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMessage("\n=== PREVIEW SEASON ORGANIZATION ===");
            LogMessage("Scanning files for season organization...\n");

            try
            {
                var seasonOperations = AnalyzeFilesForSeasons(DirectoryTextBox.Text);

                if (seasonOperations.Count == 0)
                {
                    LogMessage("No files found that need season organization.");
                    StatusTextBlock.Text = "No files to organize into seasons";
                    return;
                }

                LogMessage($"Found {seasonOperations.Count} file(s) to organize into seasons:\n");

                var groupedByFolder = seasonOperations.GroupBy(s => s.ShowFolder);
                foreach (var group in groupedByFolder)
                {
                    LogMessage($"[FOLDER] {group.Key}");
                    foreach (var season in group)
                    {
                        string folderStatus = Directory.Exists(season.SeasonFolderPath) ? "[EXISTS]" : "[WILL CREATE]";
                        LogMessage($"  {folderStatus} {season.FileName} → {season.SeasonFolder}");
                    }
                    LogMessage("");
                }

                StatusTextBlock.Text = $"Preview: {seasonOperations.Count} files ready to organize into seasons";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void OrganizeSeasonsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var seasonOperations = AnalyzeFilesForSeasons(DirectoryTextBox.Text);

            if (seasonOperations.Count == 0)
            {
                MessageBox.Show("No files found to organize into seasons.", "Nothing to Organize",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will organize {seasonOperations.Count} file(s) into season folders.\n\n" +
                "Season folders will be created if needed, and leading zeros will be removed. Continue?",
                "Confirm Season Organization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMessage("\n=== ORGANIZING INTO SEASONS ===");
            LogMessage("Starting season organization...\n");

            try
            {
                // First, rename any existing season folders with leading zeros
                RenameSeasonFoldersWithLeadingZeros(DirectoryTextBox.Text);

                int successCount = 0;
                int errorCount = 0;

                var groupedByFolder = seasonOperations.GroupBy(s => s.ShowFolder);
                foreach (var group in groupedByFolder)
                {
                    LogMessage($"[PROCESSING] {group.Key}");

                    foreach (var season in group)
                    {
                        try
                        {
                            // Create season folder if it doesn't exist
                            if (!Directory.Exists(season.SeasonFolderPath))
                            {
                                Directory.CreateDirectory(season.SeasonFolderPath);
                                LogMessage($"  [CREATED] {season.SeasonFolder}");
                            }

                            // Move file to season folder
                            string sourceFile = Path.Combine(season.ShowFolderPath, season.FileName);
                            string destFile = Path.Combine(season.SeasonFolderPath, season.FileName);

                            // Check if file already exists
                            if (File.Exists(destFile))
                            {
                                LogMessage($"  [SKIP] {season.FileName} (already exists in {season.SeasonFolder})");
                                continue;
                            }

                            File.Move(sourceFile, destFile);
                            LogMessage($"  [MOVED] {season.FileName} → {season.SeasonFolder}");
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"  [ERROR] Failed to move {season.FileName}: {ex.Message}");
                            errorCount++;
                        }
                    }

                    LogMessage("");
                }

                LogMessage($"=== COMPLETE ===");
                LogMessage($"Successfully organized: {successCount} file(s)");
                if (errorCount > 0)
                    LogMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Organized {successCount} files into seasons" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Season organization complete!\n\nSuccessfully organized: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during season organization";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateDirectory()
        {
            if (string.IsNullOrWhiteSpace(DirectoryTextBox.Text))
            {
                MessageBox.Show("Please select a directory first.", "No Directory Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Directory.Exists(DirectoryTextBox.Text))
            {
                MessageBox.Show("The selected directory does not exist.", "Invalid Directory",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private List<FileOperation> AnalyzeDirectory(string directoryPath)
        {
            var operations = new List<FileOperation>();
            var files = Directory.GetFiles(directoryPath);

            // Pattern to match: xxx.xxx.xxx.S0...* (like Show.Name.Here.S01E01.mkv)
            // This regex captures the show name before the season indicator
            var pattern = @"^(.+?)\.S\d+";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (var filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                var match = regex.Match(fileName);

                if (match.Success)
                {
                    // Extract show name and convert dots to spaces
                    string showName = match.Groups[1].Value;
                    string folderName = showName.Replace('.', ' ').Trim();

                    // Create destination folder path
                    string destinationFolder = Path.Combine(directoryPath, folderName);

                    operations.Add(new FileOperation
                    {
                        SourcePath = filePath,
                        FileName = fileName,
                        DestinationFolder = destinationFolder,
                        ShowName = folderName
                    });
                }
            }

            return operations;
        }

        private void LogMessage(string message)
        {
            LogTextBlock.Text += message + "\n";

            // Auto-scroll to bottom
            if (LogTextBlock.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        private void LogMovieMessage(string message)
        {
            MovieLogTextBlock.Text += message + "\n";

            // Auto-scroll to bottom
            if (MovieLogTextBlock.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        private void LogMusicMessage(string message)
        {
            MusicLogTextBlock.Text += message + "\n";

            // Auto-scroll to bottom
            if (MusicLogTextBlock.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        private List<FolderMerge> FindSimilarFolders(string directoryPath)
        {
            var mergeOperations = new List<FolderMerge>();
            var folders = Directory.GetDirectories(directoryPath)
                .Select(f => Path.GetFileName(f))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();

            var processedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                if (processedFolders.Contains(folder))
                    continue;

                // Find all similar folders
                var similarFolders = new List<string>();

                foreach (var otherFolder in folders)
                {
                    if (folder == otherFolder || processedFolders.Contains(otherFolder))
                        continue;

                    if (AreFoldersSimilar(folder, otherFolder, out string reason))
                    {
                        similarFolders.Add(otherFolder);
                    }
                }

                // If we found similar folders, determine which should be the target
                if (similarFolders.Count > 0)
                {
                    similarFolders.Add(folder);
                    string targetFolder = DetermineTargetFolder(similarFolders);

                    foreach (var sourceFolder in similarFolders)
                    {
                        if (sourceFolder != targetFolder)
                        {
                            string sourcePath = Path.Combine(directoryPath, sourceFolder);
                            string targetPath = Path.Combine(directoryPath, targetFolder);

                            AreFoldersSimilar(sourceFolder, targetFolder, out string mergeReason);

                            int fileCount = Directory.GetFiles(sourcePath).Length;

                            mergeOperations.Add(new FolderMerge
                            {
                                SourceFolder = sourceFolder,
                                TargetFolder = targetFolder,
                                SourceFolderPath = sourcePath,
                                TargetFolderPath = targetPath,
                                Reason = mergeReason,
                                FileCount = fileCount
                            });

                            processedFolders.Add(sourceFolder);
                        }
                    }

                    processedFolders.Add(targetFolder);
                }
            }

            return mergeOperations;
        }

        private bool AreFoldersSimilar(string folder1, string folder2, out string reason)
        {
            reason = string.Empty;

            // Normalize both folder names
            string normalized1 = NormalizeFolderName(folder1);
            string normalized2 = NormalizeFolderName(folder2);

            // Check if one is a substring of the other (after normalization)
            if (normalized1.Contains(normalized2) || normalized2.Contains(normalized1))
            {
                reason = "Similar base names";
                return true;
            }

            // Check if they differ only by year
            string withoutYear1 = RemoveYear(folder1);
            string withoutYear2 = RemoveYear(folder2);

            if (NormalizeFolderName(withoutYear1) == NormalizeFolderName(withoutYear2) && withoutYear1 != folder1)
            {
                reason = "Same name with/without year";
                return true;
            }

            // Check Levenshtein distance for very similar names
            int distance = LevenshteinDistance(normalized1, normalized2);
            int maxLength = Math.Max(normalized1.Length, normalized2.Length);

            if (maxLength > 0)
            {
                double similarity = 1.0 - ((double)distance / maxLength);
                if (similarity >= 0.85) // 85% or more similar
                {
                    reason = "Very similar names (fuzzy match)";
                    return true;
                }
            }

            return false;
        }

        private string NormalizeFolderName(string folderName)
        {
            // Remove special characters and normalize spacing
            var normalized = Regex.Replace(folderName, @"[._-]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized.Trim().ToLowerInvariant();
        }

        private string RemoveYear(string folderName)
        {
            // Remove 4-digit years (1900-2099)
            var result = Regex.Replace(folderName, @"\b(19|20)\d{2}\b", "");
            result = Regex.Replace(result, @"[._-]+", " ");
            result = Regex.Replace(result, @"\s+", " ");
            return result.Trim();
        }

        private string DetermineTargetFolder(List<string> folders)
        {
            // Prefer folder with year (more specific)
            var withYear = folders.FirstOrDefault(f => Regex.IsMatch(f, @"\b(19|20)\d{2}\b"));
            if (withYear != null)
                return withYear;

            // Prefer longer, more complete names
            return folders.OrderByDescending(f => f.Length).First();
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        private List<FileRename> AnalyzeFilesForCleaning(string directoryPath)
        {
            var renameOperations = new List<FileRename>();

            // Get all subdirectories
            var folders = Directory.GetDirectories(directoryPath);

            foreach (var folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                var files = Directory.GetFiles(folder);

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string cleanedName = CleanFileName(fileName, folderName);

                    // Only add if the name changed
                    if (cleanedName != fileName)
                    {
                        renameOperations.Add(new FileRename
                        {
                            Folder = folderName,
                            FolderPath = folder,
                            OldFileName = fileName,
                            NewFileName = cleanedName
                        });
                    }
                }
            }

            return renameOperations;
        }

        private string CleanFileName(string fileName, string folderName)
        {
            // Get file extension
            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Pattern to match season and episode: S01E01, s01e01, etc.
            var seasonEpisodePattern = @"(S\d{1,2}E\d{1,2})";
            var match = Regex.Match(nameWithoutExtension, seasonEpisodePattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                // No season/episode found, return original
                return fileName;
            }

            string seasonEpisode = match.Value;

            // Extract show name (everything before the season/episode)
            string beforeSeasonEpisode = nameWithoutExtension.Substring(0, match.Index);

            // Clean the show name:
            // 1. Remove years (4-digit numbers)
            beforeSeasonEpisode = Regex.Replace(beforeSeasonEpisode, @"\b(19|20)\d{2}\b", "");

            // 2. Replace dots, dashes, underscores with spaces
            beforeSeasonEpisode = Regex.Replace(beforeSeasonEpisode, @"[._-]", " ");

            // 3. Remove multiple spaces
            beforeSeasonEpisode = Regex.Replace(beforeSeasonEpisode, @"\s+", " ");

            // 4. Trim
            beforeSeasonEpisode = beforeSeasonEpisode.Trim();

            // If the cleaned name is empty or very short, use the folder name
            if (string.IsNullOrWhiteSpace(beforeSeasonEpisode) || beforeSeasonEpisode.Length < 2)
            {
                beforeSeasonEpisode = folderName;
            }

            // Build the clean file name: "Show Name S01E01.ext"
            string cleanName = $"{beforeSeasonEpisode} {seasonEpisode}{extension}";

            return cleanName;
        }

        private List<SeasonOperation> AnalyzeFilesForSeasons(string directoryPath)
        {
            var seasonOperations = new List<SeasonOperation>();

            // Get all subdirectories (show folders)
            var showFolders = Directory.GetDirectories(directoryPath);

            foreach (var showFolder in showFolders)
            {
                string showFolderName = Path.GetFileName(showFolder);
                var files = Directory.GetFiles(showFolder);

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);

                    // Extract season number from file name
                    var seasonMatch = Regex.Match(fileName, @"S(\d{1,2})", RegexOptions.IgnoreCase);
                    if (seasonMatch.Success)
                    {
                        int seasonNumber = int.Parse(seasonMatch.Groups[1].Value);
                        string seasonFolderName = $"Season {seasonNumber}";
                        string seasonFolderPath = Path.Combine(showFolder, seasonFolderName);

                        seasonOperations.Add(new SeasonOperation
                        {
                            ShowFolder = showFolderName,
                            ShowFolderPath = showFolder,
                            FileName = fileName,
                            SeasonNumber = seasonNumber,
                            SeasonFolder = seasonFolderName,
                            SeasonFolderPath = seasonFolderPath
                        });
                    }
                }
            }

            return seasonOperations;
        }

        private void RenameSeasonFoldersWithLeadingZeros(string directoryPath)
        {
            var showFolders = Directory.GetDirectories(directoryPath);

            foreach (var showFolder in showFolders)
            {
                var seasonFolders = Directory.GetDirectories(showFolder);

                foreach (var seasonFolder in seasonFolders)
                {
                    string folderName = Path.GetFileName(seasonFolder);

                    // Match "Season 01", "Season 02", etc. (with leading zero)
                    var match = Regex.Match(folderName, @"^Season\s+0(\d+)$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        int seasonNumber = int.Parse(match.Groups[1].Value);
                        string newFolderName = $"Season {seasonNumber}";
                        string newFolderPath = Path.Combine(showFolder, newFolderName);

                        // Only rename if the target doesn't already exist
                        if (!Directory.Exists(newFolderPath))
                        {
                            Directory.Move(seasonFolder, newFolderPath);
                            LogMessage($"[RENAMED] {folderName} → {newFolderName}");
                        }
                    }
                }
            }
        }

        private List<MovieRename> AnalyzeMoviesForCleaning(string directoryPath)
        {
            var renameOperations = new List<MovieRename>();

            // Get all files in the directory
            var files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string cleanedName = CleanMovieFileName(fileName);

                // Only add if the name changed
                if (cleanedName != fileName)
                {
                    renameOperations.Add(new MovieRename
                    {
                        FolderPath = directoryPath,
                        OldFileName = fileName,
                        NewFileName = cleanedName
                    });
                }
            }

            return renameOperations;
        }

        private string CleanMovieFileName(string fileName)
        {
            // Get file extension
            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Look for a 4-digit year (1900-2099)
            var yearPattern = @"\b(19|20)\d{2}\b";
            var yearMatch = Regex.Match(nameWithoutExtension, yearPattern);

            if (!yearMatch.Success)
            {
                // No year found, just clean up basic punctuation
                string cleaned = Regex.Replace(nameWithoutExtension, @"[._-]", " ");
                cleaned = Regex.Replace(cleaned, @"\s+", " ");
                cleaned = cleaned.Trim();
                return $"{cleaned}{extension}";
            }

            string year = yearMatch.Value;

            // Extract everything before the year
            string beforeYear = nameWithoutExtension.Substring(0, yearMatch.Index);

            // Clean the movie title:
            // 1. Replace dots, dashes, underscores with spaces
            beforeYear = Regex.Replace(beforeYear, @"[._-]", " ");

            // 2. Remove multiple spaces
            beforeYear = Regex.Replace(beforeYear, @"\s+", " ");

            // 3. Trim
            beforeYear = beforeYear.Trim();

            // If the cleaned name is empty or very short, return original
            if (string.IsNullOrWhiteSpace(beforeYear) || beforeYear.Length < 2)
            {
                return fileName;
            }

            // Build the clean file name: "Movie Title YEAR.ext"
            string cleanName = $"{beforeYear} {year}{extension}";

            return cleanName;
        }

        // ============================================================
        // MUSIC ORGANIZATION METHODS
        // ============================================================

        private void PreviewArtistsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== PREVIEW ARTIST ORGANIZATION ===");
            LogMusicMessage("Scanning directory for music files...\n");

            try
            {
                var operations = AnalyzeMusicByArtist(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMusicMessage("No music files found to organize.");
                    StatusTextBlock.Text = "No music files to organize";
                    return;
                }

                LogMusicMessage($"Found {operations.Count} music file(s) to organize:\n");

                var groupedByArtist = operations.GroupBy(o => o.ArtistFolder);
                foreach (var group in groupedByArtist)
                {
                    LogMusicMessage($"[ARTIST] {group.Key}");
                    foreach (var op in group)
                    {
                        string status = Directory.Exists(op.DestinationFolder) ? "[EXISTS]" : "[WILL CREATE]";
                        LogMusicMessage($"  {status} {op.FileName}");
                    }
                    LogMusicMessage("");
                }

                StatusTextBlock.Text = $"Preview: {operations.Count} music files ready to organize";
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void OrganizeArtistsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var operations = AnalyzeMusicByArtist(DirectoryTextBox.Text);

            if (operations.Count == 0)
            {
                MessageBox.Show("No music files found to organize.", "Nothing to Organize",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will organize {operations.Count} music file(s) into artist folders.\n\n" +
                "Files will be moved based on artist metadata and filenames. Continue?",
                "Confirm Artist Organization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMusicMessage("\n=== ORGANIZING BY ARTIST ===");
            LogMusicMessage("Starting artist organization...\n");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var op in operations)
                {
                    try
                    {
                        // Create artist folder if it doesn't exist
                        if (!Directory.Exists(op.DestinationFolder))
                        {
                            Directory.CreateDirectory(op.DestinationFolder);
                            LogMusicMessage($"[CREATED] {op.ArtistFolder}");
                        }

                        // Move file
                        string destPath = Path.Combine(op.DestinationFolder, op.FileName);
                        if (File.Exists(destPath))
                        {
                            LogMusicMessage($"[SKIP] {op.FileName} (already exists)");
                            continue;
                        }

                        File.Move(op.SourcePath, destPath);
                        LogMusicMessage($"[MOVED] {op.FileName} → {op.ArtistFolder}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMusicMessage($"[ERROR] {op.FileName}: {ex.Message}");
                        errorCount++;
                    }
                }

                LogMusicMessage($"\n=== COMPLETE ===");
                LogMusicMessage($"Successfully organized: {successCount} file(s)");
                if (errorCount > 0)
                    LogMusicMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Organized {successCount} music files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Artist organization complete!\n\nSuccessfully organized: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during organization";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewMusicCleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== PREVIEW FILENAME CLEANING ===");
            LogMusicMessage("Scanning music files for cleaning...\n");

            try
            {
                var operations = AnalyzeMusicForCleaning(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMusicMessage("No music files found that need cleaning.");
                    StatusTextBlock.Text = "No music files to clean";
                    return;
                }

                LogMusicMessage($"Found {operations.Count} music file(s) to clean:\n");

                foreach (var op in operations.GroupBy(o => o.FolderName))
                {
                    LogMusicMessage($"[FOLDER] {op.Key}");
                    foreach (var rename in op)
                    {
                        LogMusicMessage($"  FROM: {rename.OldFileName}");
                        LogMusicMessage($"  TO:   {rename.NewFileName}");
                    }
                    LogMusicMessage("");
                }

                StatusTextBlock.Text = $"Preview: {operations.Count} music files ready to clean";
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private void CleanMusicFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            var operations = AnalyzeMusicForCleaning(DirectoryTextBox.Text);

            if (operations.Count == 0)
            {
                MessageBox.Show("No music files found that need cleaning.", "Nothing to Clean",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"This will rename {operations.Count} music file(s) to 'Artist - Song Title' format.\n\n" +
                "Track numbers and extra text will be removed. Continue?",
                "Confirm Filename Cleaning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            LogMusicMessage("\n=== CLEANING MUSIC FILENAMES ===");
            LogMusicMessage("Starting filename cleaning...\n");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var op in operations)
                {
                    try
                    {
                        string oldPath = Path.Combine(op.FolderPath, op.OldFileName);
                        string newPath = Path.Combine(op.FolderPath, op.NewFileName);

                        if (File.Exists(newPath))
                        {
                            LogMusicMessage($"[SKIP] {op.OldFileName} (target exists)");
                            continue;
                        }

                        File.Move(oldPath, newPath);
                        LogMusicMessage($"[RENAMED] {op.OldFileName} → {op.NewFileName}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMusicMessage($"[ERROR] {op.OldFileName}: {ex.Message}");
                        errorCount++;
                    }
                }

                LogMusicMessage($"\n=== COMPLETE ===");
                LogMusicMessage($"Successfully cleaned: {successCount} file(s)");
                if (errorCount > 0)
                    LogMusicMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Cleaned {successCount} music files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Filename cleaning complete!\n\nSuccessfully cleaned: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during cleaning";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PreviewAlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== PREVIEW ALBUM ORGANIZATION ===");
            LogMusicMessage("Analyzing music files and looking up album information online...\n");
            LogMusicMessage("This may take a moment...\n");

            try
            {
                var operations = await AnalyzeMusicForAlbums(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMusicMessage("No music files found for album organization.");
                    StatusTextBlock.Text = "No music files found";
                    return;
                }

                LogMusicMessage($"Found {operations.Count} track(s) to organize into albums:\n");

                var groupedByArtist = operations.GroupBy(o => o.Artist);
                foreach (var artistGroup in groupedByArtist)
                {
                    LogMusicMessage($"[ARTIST] {artistGroup.Key}");
                    var groupedByAlbum = artistGroup.GroupBy(o => o.Album);
                    foreach (var albumGroup in groupedByAlbum)
                    {
                        LogMusicMessage($"  [ALBUM] {albumGroup.Key}");
                        foreach (var track in albumGroup)
                        {
                            LogMusicMessage($"    {track.TrackNumber:D2} - {track.Title}");
                        }
                    }
                    LogMusicMessage("");
                }

                StatusTextBlock.Text = $"Preview: {operations.Count} tracks ready to organize";
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private async void OrganizeAlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== ANALYZING FOR ALBUM ORGANIZATION ===");
            LogMusicMessage("Looking up album information online...\n");

            try
            {
                var operations = await AnalyzeMusicForAlbums(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    MessageBox.Show("No music files found for album organization.", "Nothing to Organize",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will organize {operations.Count} track(s) into album folders.\n\n" +
                    "Album folders will be created and files renamed with track numbers. Continue?",
                    "Confirm Album Organization",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                LogMusicMessage("\n=== ORGANIZING BY ALBUMS ===");
                LogMusicMessage("Creating album folders and organizing tracks...\n");

                int successCount = 0;
                int errorCount = 0;

                foreach (var op in operations)
                {
                    try
                    {
                        // Create album folder if it doesn't exist
                        if (!Directory.Exists(op.AlbumFolderPath))
                        {
                            Directory.CreateDirectory(op.AlbumFolderPath);
                            LogMusicMessage($"[CREATED] {op.Artist}/{op.Album}");
                        }

                        // Move and rename file
                        string destPath = Path.Combine(op.AlbumFolderPath, op.NewFileName);
                        if (File.Exists(destPath))
                        {
                            LogMusicMessage($"[SKIP] {op.NewFileName} (already exists)");
                            continue;
                        }

                        File.Move(op.SourcePath, destPath);
                        LogMusicMessage($"[MOVED] {op.Title} → {op.Artist}/{op.Album}/{op.NewFileName}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMusicMessage($"[ERROR] {op.Title}: {ex.Message}");
                        errorCount++;
                    }
                }

                LogMusicMessage($"\n=== COMPLETE ===");
                LogMusicMessage($"Successfully organized: {successCount} track(s)");
                if (errorCount > 0)
                    LogMusicMessage($"Errors: {errorCount} track(s)");

                StatusTextBlock.Text = $"Organized {successCount} tracks" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Album organization complete!\n\nSuccessfully organized: {successCount} tracks\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during organization";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PreviewMetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== PREVIEW METADATA UPDATES ===");
            LogMusicMessage("Analyzing music files and looking up metadata online...\n");
            LogMusicMessage("This may take a moment...\n");

            try
            {
                var operations = await AnalyzeMusicForMetadataUpdate(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    LogMusicMessage("No music files found for metadata update.");
                    StatusTextBlock.Text = "No music files found";
                    return;
                }

                LogMusicMessage($"Found {operations.Count} file(s) with metadata updates:\n");

                foreach (var op in operations)
                {
                    LogMusicMessage($"[FILE] {op.FileName}");
                    if (!string.IsNullOrEmpty(op.NewArtist))
                        LogMusicMessage($"  Artist: {op.OldArtist} → {op.NewArtist}");
                    if (!string.IsNullOrEmpty(op.NewTitle))
                        LogMusicMessage($"  Title: {op.OldTitle} → {op.NewTitle}");
                    if (!string.IsNullOrEmpty(op.NewAlbum))
                        LogMusicMessage($"  Album: {op.OldAlbum} → {op.NewAlbum}");
                    if (op.NewYear > 0)
                        LogMusicMessage($"  Year: {op.OldYear} → {op.NewYear}");
                    LogMusicMessage("");
                }

                StatusTextBlock.Text = $"Preview: {operations.Count} files ready for metadata update";
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during preview";
            }
        }

        private async void UpdateMetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDirectory())
                return;

            LogMusicMessage("\n=== ANALYZING METADATA ===");
            LogMusicMessage("Looking up metadata online...\n");

            try
            {
                var operations = await AnalyzeMusicForMetadataUpdate(DirectoryTextBox.Text);

                if (operations.Count == 0)
                {
                    MessageBox.Show("No music files found for metadata update.", "Nothing to Update",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will update metadata for {operations.Count} file(s) using online database.\n\n" +
                    "Existing metadata will be updated with accurate information. Continue?",
                    "Confirm Metadata Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                LogMusicMessage("\n=== UPDATING METADATA ===");
                LogMusicMessage("Writing metadata to files...\n");

                int successCount = 0;
                int errorCount = 0;

                foreach (var op in operations)
                {
                    try
                    {
                        using (var file = TagLib.File.Create(op.FilePath))
                        {
                            if (!string.IsNullOrEmpty(op.NewArtist))
                                file.Tag.Performers = new[] { op.NewArtist };
                            if (!string.IsNullOrEmpty(op.NewTitle))
                                file.Tag.Title = op.NewTitle;
                            if (!string.IsNullOrEmpty(op.NewAlbum))
                                file.Tag.Album = op.NewAlbum;
                            if (op.NewYear > 0)
                                file.Tag.Year = (uint)op.NewYear;
                            if (op.NewTrackNumber > 0)
                                file.Tag.Track = (uint)op.NewTrackNumber;

                            file.Save();
                        }

                        LogMusicMessage($"[UPDATED] {op.FileName}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMusicMessage($"[ERROR] {op.FileName}: {ex.Message}");
                        errorCount++;
                    }
                }

                LogMusicMessage($"\n=== COMPLETE ===");
                LogMusicMessage($"Successfully updated: {successCount} file(s)");
                if (errorCount > 0)
                    LogMusicMessage($"Errors: {errorCount} file(s)");

                StatusTextBlock.Text = $"Updated metadata for {successCount} files" + (errorCount > 0 ? $" ({errorCount} errors)" : "");

                MessageBox.Show(
                    $"Metadata update complete!\n\nSuccessfully updated: {successCount} files\nErrors: {errorCount}",
                    "Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMusicMessage($"ERROR: {ex.Message}");
                StatusTextBlock.Text = "Error during metadata update";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // MUSIC HELPER METHODS
        // ============================================================

        private List<MusicOrganizeOperation> AnalyzeMusicByArtist(string directoryPath)
        {
            var operations = new List<MusicOrganizeOperation>();
            var musicFiles = GetMusicFiles(directoryPath);

            foreach (var file in musicFiles)
            {
                string fileName = Path.GetFileName(file);
                string artist = GetArtistFromFile(file);

                if (string.IsNullOrWhiteSpace(artist))
                    continue;

                string cleanArtist = CleanArtistName(artist);
                string artistFolder = Path.Combine(directoryPath, cleanArtist);

                operations.Add(new MusicOrganizeOperation
                {
                    SourcePath = file,
                    FileName = fileName,
                    ArtistFolder = cleanArtist,
                    DestinationFolder = artistFolder
                });
            }

            return operations;
        }

        private List<MusicCleanOperation> AnalyzeMusicForCleaning(string directoryPath)
        {
            var operations = new List<MusicCleanOperation>();
            var folders = Directory.GetDirectories(directoryPath);

            foreach (var folder in folders)
            {
                var musicFiles = GetMusicFiles(folder);
                string folderName = Path.GetFileName(folder);

                foreach (var file in musicFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string cleanedName = CleanMusicFileName(file);

                    if (cleanedName != fileName)
                    {
                        operations.Add(new MusicCleanOperation
                        {
                            FolderPath = folder,
                            FolderName = folderName,
                            OldFileName = fileName,
                            NewFileName = cleanedName
                        });
                    }
                }
            }

            return operations;
        }

        private async Task<List<AlbumOrganizeOperation>> AnalyzeMusicForAlbums(string directoryPath)
        {
            var operations = new List<AlbumOrganizeOperation>();
            var folders = Directory.GetDirectories(directoryPath);

            foreach (var folder in folders)
            {
                var musicFiles = GetMusicFiles(folder);
                string artistFolder = Path.GetFileName(folder);

                foreach (var file in musicFiles)
                {
                    try
                    {
                        var trackInfo = await LookupTrackInfo(file);
                        if (trackInfo == null)
                            continue;

                        string albumFolder = Path.Combine(folder, CleanArtistName(trackInfo.Album));
                        string newFileName = $"{trackInfo.TrackNumber:D2} - {trackInfo.Title}{Path.GetExtension(file)}";

                        operations.Add(new AlbumOrganizeOperation
                        {
                            SourcePath = file,
                            Artist = trackInfo.Artist,
                            Album = trackInfo.Album,
                            Title = trackInfo.Title,
                            TrackNumber = trackInfo.TrackNumber,
                            AlbumFolderPath = albumFolder,
                            NewFileName = newFileName
                        });
                    }
                    catch
                    {
                        // Skip files that fail to lookup
                        continue;
                    }
                }
            }

            return operations;
        }

        private async Task<List<MetadataUpdateOperation>> AnalyzeMusicForMetadataUpdate(string directoryPath)
        {
            var operations = new List<MetadataUpdateOperation>();
            var musicFiles = GetMusicFilesRecursive(directoryPath);

            foreach (var file in musicFiles)
            {
                try
                {
                    var trackInfo = await LookupTrackInfo(file);
                    if (trackInfo == null)
                        continue;

                    // Read current metadata
                    string oldArtist = "", oldTitle = "", oldAlbum = "";
                    int oldYear = 0;

                    using (var tagFile = TagLib.File.Create(file))
                    {
                        oldArtist = tagFile.Tag.FirstPerformer ?? "";
                        oldTitle = tagFile.Tag.Title ?? "";
                        oldAlbum = tagFile.Tag.Album ?? "";
                        oldYear = (int)(tagFile.Tag.Year);
                    }

                    // Only add if there are differences
                    if (trackInfo.Artist != oldArtist || trackInfo.Title != oldTitle ||
                        trackInfo.Album != oldAlbum || trackInfo.Year != oldYear)
                    {
                        operations.Add(new MetadataUpdateOperation
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            OldArtist = oldArtist,
                            NewArtist = trackInfo.Artist,
                            OldTitle = oldTitle,
                            NewTitle = trackInfo.Title,
                            OldAlbum = oldAlbum,
                            NewAlbum = trackInfo.Album,
                            OldYear = oldYear,
                            NewYear = trackInfo.Year,
                            NewTrackNumber = trackInfo.TrackNumber
                        });
                    }
                }
                catch
                {
                    // Skip files that fail
                    continue;
                }
            }

            return operations;
        }

        private string GetArtistFromFile(string filePath)
        {
            try
            {
                // Try to read from metadata first
                using (var file = TagLib.File.Create(filePath))
                {
                    string artist = file.Tag.FirstPerformer;
                    if (!string.IsNullOrWhiteSpace(artist))
                        return artist;
                }
            }
            catch
            {
                // If metadata reading fails, try filename
            }

            // Fallback: Try to extract from filename
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Check for "Artist - Title" format
            if (fileName.Contains(" - "))
            {
                return fileName.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
            }

            // Check for track number prefix "01. Artist - Title"
            var match = Regex.Match(fileName, @"^\d+\.\s*(.+?)\s*-\s*.+$");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return "Unknown Artist";
        }

        private string CleanArtistName(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
                return "Unknown Artist";

            // Remove brackets and their contents
            artistName = Regex.Replace(artistName, @"\[.*?\]", "");
            artistName = Regex.Replace(artistName, @"\(.*?\)", "");

            // Remove leading zeros (e.g., "01 - Artist" → "Artist")
            artistName = Regex.Replace(artistName, @"^\d+\s*[-.]?\s*", "");

            // Remove years (4-digit numbers)
            artistName = Regex.Replace(artistName, @"\b(19|20)\d{2}\b", "");

            // Clean punctuation
            artistName = Regex.Replace(artistName, @"[._]", " ");
            artistName = Regex.Replace(artistName, @"\s+", " ");

            // Remove "feat.", "ft.", "featuring", etc.
            artistName = Regex.Replace(artistName, @"\b(feat|ft|featuring)\.?\b.*$", "", RegexOptions.IgnoreCase);

            return artistName.Trim();
        }

        private string CleanMusicFileName(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            string artist = "";
            string title = "";

            try
            {
                // Try to read from metadata
                using (var file = TagLib.File.Create(filePath))
                {
                    artist = file.Tag.FirstPerformer ?? "";
                    title = file.Tag.Title ?? "";
                }

                if (!string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title))
                {
                    artist = CleanArtistName(artist);
                    title = title.Trim();
                    return $"{artist} - {title}{extension}";
                }
            }
            catch
            {
                // Fallback to filename parsing
            }

            // Parse from filename
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Remove track numbers from the start
            fileName = Regex.Replace(fileName, @"^\d+\s*[-.]?\s*", "");

            // If it already has "Artist - Title" format, just clean it
            if (fileName.Contains(" - "))
            {
                var parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                artist = CleanArtistName(parts[0]);
                title = parts[1].Trim();
            }
            else
            {
                // Use folder name as artist, filename as title
                string folderName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
                artist = CleanArtistName(folderName);
                title = fileName;
            }

            return $"{artist} - {title}{extension}";
        }

        private List<string> GetMusicFiles(string directoryPath)
        {
            var musicExtensions = new[] { ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".wav", ".wma" };
            var musicFiles = new List<string>();

            try
            {
                var allFiles = Directory.GetFiles(directoryPath);
                foreach (var file in allFiles)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (musicExtensions.Contains(ext))
                    {
                        musicFiles.Add(file);
                    }
                }
            }
            catch
            {
                // Return empty list if directory can't be read
            }

            return musicFiles;
        }

        private List<string> GetMusicFilesRecursive(string directoryPath)
        {
            var musicExtensions = new[] { ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".wav", ".wma" };
            var musicFiles = new List<string>();

            try
            {
                var allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (musicExtensions.Contains(ext))
                    {
                        musicFiles.Add(file);
                    }
                }
            }
            catch
            {
                // Return what we have if error occurs
            }

            return musicFiles;
        }

        private async Task<TrackInfo?> LookupTrackInfo(string filePath)
        {
            try
            {
                // First, try to read metadata from the file
                string artist = "";
                string title = "";
                string album = "";
                int year = 0;
                int trackNumber = 0;

                using (var file = TagLib.File.Create(filePath))
                {
                    artist = file.Tag.FirstPerformer ?? "";
                    title = file.Tag.Title ?? "";
                    album = file.Tag.Album ?? "";
                    year = (int)(file.Tag.Year);
                    trackNumber = (int)(file.Tag.Track);
                }

                // If we have enough metadata, use it
                if (!string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title))
                {
                    // If album is missing, try to look it up online
                    if (string.IsNullOrWhiteSpace(album))
                    {
                        var onlineInfo = await LookupMusicBrainz(artist, title);
                        if (onlineInfo != null)
                        {
                            album = onlineInfo.Album;
                            if (year == 0) year = onlineInfo.Year;
                            if (trackNumber == 0) trackNumber = onlineInfo.TrackNumber;
                        }
                    }

                    return new TrackInfo
                    {
                        Artist = artist,
                        Title = title,
                        Album = album ?? "Unknown Album",
                        Year = year,
                        TrackNumber = trackNumber > 0 ? trackNumber : 1
                    };
                }

                // If metadata is missing, try to parse filename and lookup online
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                fileName = Regex.Replace(fileName, @"^\d+\s*[-.]?\s*", ""); // Remove track numbers

                if (fileName.Contains(" - "))
                {
                    var parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    artist = parts[0].Trim();
                    title = parts[1].Trim();

                    var onlineInfo = await LookupMusicBrainz(artist, title);
                    if (onlineInfo != null)
                        return onlineInfo;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<TrackInfo?> LookupMusicBrainz(string artist, string title)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set user agent (required by MusicBrainz API)
                    client.DefaultRequestHeaders.Add("User-Agent", "FileSorter/1.0 (https://github.com/example/filesorter)");

                    // Search for the recording
                    string query = Uri.EscapeDataString($"artist:\"{artist}\" AND recording:\"{title}\"");
                    string url = $"https://musicbrainz.org/ws/2/recording?query={query}&fmt=json&limit=1";

                    var response = await client.GetStringAsync(url);
                    var json = JObject.Parse(response);

                    var recordings = json["recordings"] as JArray;
                    if (recordings == null || recordings.Count == 0)
                        return null;

                    var recording = recordings[0];
                    var releases = recording["releases"] as JArray;

                    string albumName = releases != null && releases.Count > 0
                        ? releases[0]["title"]?.ToString() ?? "Unknown Album"
                        : "Unknown Album";

                    int releaseYear = 0;
                    if (releases != null && releases.Count > 0)
                    {
                        string? date = releases[0]["date"]?.ToString();
                        if (!string.IsNullOrEmpty(date) && date.Length >= 4)
                        {
                            int.TryParse(date.Substring(0, 4), out releaseYear);
                        }
                    }

                    return new TrackInfo
                    {
                        Artist = recording["artist-credit"]?[0]?["name"]?.ToString() ?? artist,
                        Title = recording["title"]?.ToString() ?? title,
                        Album = albumName,
                        Year = releaseYear,
                        TrackNumber = 1 // MusicBrainz doesn't always provide track number in this query
                    };
                }
            }
            catch
            {
                // If online lookup fails, return null
                return null;
            }
        }

        private class TrackInfo
        {
            public string Artist { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Album { get; set; } = string.Empty;
            public int Year { get; set; }
            public int TrackNumber { get; set; }
        }

        private class FileOperation
        {
            public string SourcePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string DestinationFolder { get; set; } = string.Empty;
            public string ShowName { get; set; } = string.Empty;
        }

        private class FolderMerge
        {
            public string SourceFolder { get; set; } = string.Empty;
            public string TargetFolder { get; set; } = string.Empty;
            public string SourceFolderPath { get; set; } = string.Empty;
            public string TargetFolderPath { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public int FileCount { get; set; }
        }

        private class FileRename
        {
            public string Folder { get; set; } = string.Empty;
            public string FolderPath { get; set; } = string.Empty;
            public string OldFileName { get; set; } = string.Empty;
            public string NewFileName { get; set; } = string.Empty;
        }

        private class SeasonOperation
        {
            public string ShowFolder { get; set; } = string.Empty;
            public string ShowFolderPath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public int SeasonNumber { get; set; }
            public string SeasonFolder { get; set; } = string.Empty;
            public string SeasonFolderPath { get; set; } = string.Empty;
        }

        private class MovieRename
        {
            public string FolderPath { get; set; } = string.Empty;
            public string OldFileName { get; set; } = string.Empty;
            public string NewFileName { get; set; } = string.Empty;
        }

        private class MusicOrganizeOperation
        {
            public string SourcePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string ArtistFolder { get; set; } = string.Empty;
            public string DestinationFolder { get; set; } = string.Empty;
        }

        private class MusicCleanOperation
        {
            public string FolderPath { get; set; } = string.Empty;
            public string FolderName { get; set; } = string.Empty;
            public string OldFileName { get; set; } = string.Empty;
            public string NewFileName { get; set; } = string.Empty;
        }

        private class AlbumOrganizeOperation
        {
            public string SourcePath { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;
            public string Album { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int TrackNumber { get; set; }
            public string AlbumFolderPath { get; set; } = string.Empty;
            public string NewFileName { get; set; } = string.Empty;
        }

        private class MetadataUpdateOperation
        {
            public string FilePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string OldArtist { get; set; } = string.Empty;
            public string NewArtist { get; set; } = string.Empty;
            public string OldTitle { get; set; } = string.Empty;
            public string NewTitle { get; set; } = string.Empty;
            public string OldAlbum { get; set; } = string.Empty;
            public string NewAlbum { get; set; } = string.Empty;
            public int OldYear { get; set; }
            public int NewYear { get; set; }
            public int NewTrackNumber { get; set; }
        }
    }
}
