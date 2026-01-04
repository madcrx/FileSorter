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
            StatusTextBlock.Text = "Ready";
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
    }
}
