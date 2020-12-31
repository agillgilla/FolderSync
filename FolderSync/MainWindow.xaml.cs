using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace FolderSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool UseDateModified;
        public static bool HideSyncedFiles;

        public List<FileNode> SourceNodes { get; set; }

        public List<FileNode> DestNodes { get; set; }

        private string SourceDir { get; set; }

        private string DestDir { get; set; }

        public MainWindow() {
            InitializeComponent();
            DataContext = this;

            HideSyncedFiles = (bool)HideSyncedCheckbox.IsChecked;
            UseDateModified = (bool) UseDateCheckbox.IsChecked;

            SourceTextbox.KeyDown += SourceTextbox_KeyDown;
            DestTextbox.KeyDown += DestTextbox_KeyDown;
        }

        private void SourceTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                if (SourceTextbox.Text.Trim() != "") {
                    if (Directory.Exists(SourceTextbox.Text)) {
                        SourceDir = SourceTextbox.Text;
                        ListSourceFiles(SourceTextbox.Text);
                    } else {
                        System.Windows.MessageBox.Show(string.Format("The directory {0} could not be found", SourceTextbox.Text), "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void DestTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                if (DestTextbox.Text.Trim() != "") {
                    if (Directory.Exists(DestTextbox.Text)) {
                        DestDir = SourceTextbox.Text;
                        ListDestFiles(DestTextbox.Text);
                    } else {
                        System.Windows.MessageBox.Show(string.Format("The directory {0} could not be found", DestTextbox.Text), "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void ListSourceFiles(string path) {
            ListDirectory(path, true);
        }

        private void ListDestFiles(string path) {
            ListDirectory(path, false);
        }


        private void ListDirectory(string path, bool isSource) {

            Dictionary<string, BitmapSource> iconSources = new Dictionary<string, BitmapSource>();

            if (isSource) {
                if (SourceNodes != null) {
                    SourceNodes.Clear();
                }
            } else {
                if (DestNodes != null) {
                    DestNodes.Clear();
                }
            }

            var stack = new Stack<FileNode>();
            var rootDirectory = new DirectoryInfo(path);
            var node = new FileNode {
                DirInfo = rootDirectory,
                Filepath = rootDirectory.FullName,
                RelativePath = "",
                Name = rootDirectory.Name,
                Children = new List<FileNode>(),
                IsExpanded = true,
                SyncState = SyncState.None
            };

            stack.Push(node);

            while (stack.Count > 0) {
                var currentNode = stack.Pop();
                var directoryInfo = (DirectoryInfo)currentNode.DirInfo;
                foreach (var directory in directoryInfo.EnumerateDirectories().OrderBy(d => d.Name)) {
                    var childDirectoryNode = new FileNode {
                        DirInfo = directory,
                        Parent = currentNode,
                        Filepath = directory.FullName,
                        RelativePath = Path.Combine(currentNode.RelativePath, directory.Name),
                        Name = directory.Name,
                        Children = new List<FileNode>(),
                        SyncState = SyncState.None
                    };
                    currentNode.Children.Add(childDirectoryNode);
                    stack.Push(childDirectoryNode);
                }
                foreach (var file in directoryInfo.EnumerateFiles().OrderBy(f => f.Name)) {

                    BitmapSource iconSource;

                    if (!iconSources.TryGetValue(file.Extension, out iconSource)) {
                        iconSource = IconExtensions.GetIconAsBitmapSource(file.Extension, new System.Drawing.Size(16, 16));
                        iconSources.Add(file.Extension, iconSource);
                    }

                    var newNode = new FileNode {
                        FileInfo = file,
                        Parent = currentNode,
                        Filepath = file.FullName,
                        RelativePath = Path.Combine(currentNode.RelativePath, file.Name),
                        Name = file.Name,
                        Icon = iconSource,
                        SyncState = SyncState.None
                    };
                    currentNode.Children.Add(newNode);
                }

            }

            if (isSource) {
                SourceNodes = new List<FileNode> { node };
                if (SourceTree.GetBindingExpression(ItemsControl.ItemsSourceProperty) != null) {
                    SourceTree.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
                }
            } else {
                DestNodes = new List<FileNode> { node };
                if (DestTree.GetBindingExpression(ItemsControl.ItemsSourceProperty) != null) {
                    DestTree.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
                }
            }
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e) {
            using (var dialog = new FolderBrowserDialog()) {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    SourceDir = dialog.SelectedPath;
                    ListSourceFiles(dialog.SelectedPath);
                    SourceTextbox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseDestButton_Click(object sender, RoutedEventArgs e) {
            using (var dialog = new FolderBrowserDialog()) {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    DestDir = dialog.SelectedPath;
                    ListDestFiles(dialog.SelectedPath);
                    DestTextbox.Text = dialog.SelectedPath;
                }
            }
        }

        private void UseDateCheckbox_Changed(object sender, RoutedEventArgs e) {
            UseDateModified = (bool)UseDateCheckbox.IsChecked;
        }

        private void HideSyncedCheckbox_Changed(object sender, RoutedEventArgs e) {
            HideSyncedFiles = (bool)HideSyncedCheckbox.IsChecked;

            Stack<FileNode> sourceStack = new Stack<FileNode>();
            sourceStack.Push(SourceNodes[0]);

            FileNode sourceFileNode = sourceStack.Pop();
            sourceFileNode.NotifyPropertyChanged("SyncState");
            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                sourceStack.Push(sourceFileNode.Children[i]);
            }

            while (sourceStack.Count > 0) {
                sourceFileNode = sourceStack.Pop();

                sourceFileNode.NotifyPropertyChanged("SyncState");

                if (sourceFileNode.DirInfo != null) {
                    for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                        sourceStack.Push(sourceFileNode.Children[i]);
                    }
                }
            }

            Stack<FileNode> destStack = new Stack<FileNode>();
            destStack.Push(DestNodes[0]);

            FileNode destFileNode = destStack.Pop();
            destFileNode.NotifyPropertyChanged("SyncState");
            for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                destStack.Push(destFileNode.Children[i]);
            }

            while (destStack.Count > 0) {
                destFileNode = destStack.Pop();

                destFileNode.NotifyPropertyChanged("SyncState");

                if (destFileNode.DirInfo != null) {
                    for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                        destStack.Push(destFileNode.Children[i]);
                    }
                }
            }
        }

        private void analyzeButton_Click(object sender, RoutedEventArgs e) {
            if (SourceNodes == null || SourceNodes.Count < 1) {
                System.Windows.MessageBox.Show("There are no source files", "No Source Files", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            } else if (DestNodes == null || DestNodes.Count < 1) {
                System.Windows.MessageBox.Show("There are no destination files", "No Destination Files", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            } else {

                Stack<FileNode> sourceStack = new Stack<FileNode>();
                Stack<FileNode> destStack = new Stack<FileNode>();

                sourceStack.Push(SourceNodes[0]);
                destStack.Push(DestNodes[0]);

                FileNode sourceFileNode = sourceStack.Pop();
                for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                    sourceStack.Push(sourceFileNode.Children[i]);
                }

                FileNode destFileNode = destStack.Pop();
                for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                    destStack.Push(destFileNode.Children[i]);
                }

                while (sourceStack.Count > 0 && destStack.Count > 0) {
                    if (sourceFileNode.RelativePath.CompareTo(destFileNode.RelativePath) < 0) {
                        sourceFileNode.SyncState = SyncState.New;

                        sourceFileNode = sourceStack.Pop();

                        if (sourceFileNode.DirInfo != null) {
                            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                                sourceStack.Push(sourceFileNode.Children[i]);
                            }
                        } else {

                        }
                    } else if (sourceFileNode.RelativePath.CompareTo(destFileNode.RelativePath) > 0) {
                        destFileNode.SyncState = SyncState.New;

                        destFileNode = destStack.Pop();

                        if (destFileNode.DirInfo != null) {
                            for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                                destStack.Push(destFileNode.Children[i]);
                            }
                        } else {

                        }
                    } else {
                        // TODO: Check file size
                        sourceFileNode.SyncState = SyncState.Synced;
                        destFileNode.SyncState = SyncState.Synced;

                        sourceFileNode = sourceStack.Pop();

                        if (sourceFileNode.DirInfo != null) {
                            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                                sourceStack.Push(sourceFileNode.Children[i]);
                            }
                        } else {

                        }

                        destFileNode = destStack.Pop();

                        if (destFileNode.DirInfo != null) {
                            for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                                destStack.Push(destFileNode.Children[i]);
                            }
                        } else {

                        }
                    }
                }

                while (sourceStack.Count > 0) {
                    if (sourceFileNode.RelativePath.CompareTo(destFileNode.RelativePath) == 0) {
                        // TODO: Check file size
                        sourceFileNode.SyncState = SyncState.Synced;
                        destFileNode.SyncState = SyncState.Synced;
                        Console.WriteLine("Setting to synced");

                        sourceFileNode = sourceStack.Pop();

                        if (sourceFileNode.DirInfo != null) {
                            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                                sourceStack.Push(sourceFileNode.Children[i]);
                            }
                        } else {

                        }
                    } else {
                        sourceFileNode.SyncState = SyncState.New;

                        sourceFileNode = sourceStack.Pop();

                        if (sourceFileNode.DirInfo != null) {
                            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                                sourceStack.Push(sourceFileNode.Children[i]);
                            }
                        } else {

                        }
                    }
                } 

                while (destStack.Count > 0) {
                    if (destFileNode.RelativePath.CompareTo(sourceFileNode.RelativePath) == 0) {
                        // TODO: Check file size
                        sourceFileNode.SyncState = SyncState.Synced;
                        destFileNode.SyncState = SyncState.Synced;
                        Console.WriteLine("Setting to synced");

                        destFileNode = destStack.Pop();

                        if (destFileNode.DirInfo != null) {
                            for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                                destStack.Push(destFileNode.Children[i]);
                            }
                        } else {

                        }
                    } else {
                        destFileNode.SyncState = SyncState.New;

                        destFileNode = destStack.Pop();

                        if (destFileNode.DirInfo != null) {
                            for (int i = destFileNode.Children.Count - 1; i >= 0; i--) {
                                destStack.Push(destFileNode.Children[i]);
                            }
                        } else {

                        }
                    }
                }

                if (sourceFileNode.RelativePath.CompareTo(destFileNode.RelativePath) == 0) {
                    // TODO: Check file size
                    sourceFileNode.SyncState = SyncState.Synced;
                    destFileNode.SyncState = SyncState.Synced;
                    Console.WriteLine("Setting to synced");
                } else {
                    if (sourceFileNode.SyncState == SyncState.None) {
                        sourceFileNode.SyncState = SyncState.New;
                    }
                    if (destFileNode.SyncState == SyncState.None) {
                        destFileNode.SyncState = SyncState.New;
                    }
                }


                if (SourceTree.GetBindingExpression(ItemsControl.ItemsSourceProperty) != null) {
                    SourceTree.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
                }
                if (DestTree.GetBindingExpression(ItemsControl.ItemsSourceProperty) != null) {
                    DestTree.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
                }

                syncButton.IsEnabled = true;
            }
        }

        public List<SyncOperation> ComputeSyncOperations() {

            List<SyncOperation> syncOperations = new List<SyncOperation>();

            Stack<FileNode> sourceStack = new Stack<FileNode>();
            sourceStack.Push(SourceNodes[0]);

            FileNode sourceFileNode = sourceStack.Pop();
            for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                sourceStack.Push(sourceFileNode.Children[i]);
            }

            while (sourceStack.Count > 0) {
                sourceFileNode = sourceStack.Pop();

                if (sourceFileNode.SyncState != SyncState.Synced) {
                    Console.WriteLine("{0} --> {1}", sourceFileNode.Filepath, Path.Combine(DestDir, sourceFileNode.RelativePath));

                    syncOperations.Add(new SyncOperation(sourceFileNode.Filepath, Path.Combine(DestDir, sourceFileNode.RelativePath)));
                }

                if (sourceFileNode.DirInfo != null) {
                    for (int i = sourceFileNode.Children.Count - 1; i >= 0; i--) {
                        sourceStack.Push(sourceFileNode.Children[i]);
                    }
                }  
            }

            return syncOperations;
        }

        public void ExecuteSyncOperations(List<SyncOperation> syncOperations) {
            foreach (SyncOperation syncOperation in syncOperations) {
                File.Copy(syncOperation.SourceFilepath, syncOperation.DestFilepath, true);
            }
        }

        private void syncButton_Click(object sender, RoutedEventArgs e) {
            List<SyncOperation> syncOperations = ComputeSyncOperations();

            ExecuteSyncOperations(syncOperations);

            System.Windows.MessageBox.Show("Sync Completed Successfully.", "Sync Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class SyncOperation
    {
        public string SourceFilepath { get; set; }

        public string DestFilepath { get; set; }

        public SyncOperation(string sourceFilepath, string destFilepath) {
            SourceFilepath = sourceFilepath;
            DestFilepath = destFilepath;
        }
    }

    public enum SyncState
    {
        None,
        New,
        Synced,
        Modified
    }

    public class FileNode : INotifyPropertyChanged
    {
        public FileInfo FileInfo { get; set; }
        public DirectoryInfo DirInfo { get; set; }
        public FileNode Parent { get; set; }
        public string Filepath { get; set; }
        public string RelativePath { get; set; }
        public string Name { get; set; }
        public BitmapSource Icon { get; set; }
        public List<FileNode> Children { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        private SyncState _syncState;
        public SyncState SyncState {
            get { return _syncState;  }
            set {
                _syncState = value;
                NotifyPropertyChanged("SyncState");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equivalent(FileNode other) {
            if (FileInfo != null && other.FileInfo != null) {
                if (MainWindow.UseDateModified) {
                    return FileInfo.Length == other.FileInfo.Length && FileInfo.LastWriteTime == other.FileInfo.LastWriteTime;
                }
                return FileInfo.Length == other.FileInfo.Length;
            } else if (DirInfo != null && other.DirInfo != null) {
                if (MainWindow.UseDateModified) {
                    return DirInfo.LastWriteTime == other.DirInfo.LastWriteTime;
                }
                return true;
            }

            return false;
        }
    }
}
