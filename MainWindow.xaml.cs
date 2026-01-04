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

        private class FileOperation
        {
            public string SourcePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string DestinationFolder { get; set; } = string.Empty;
            public string ShowName { get; set; } = string.Empty;
        }
    }
}
