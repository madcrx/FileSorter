using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

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
    }
}
