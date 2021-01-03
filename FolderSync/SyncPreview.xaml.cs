using System;
using System.Collections.Generic;
using System.Windows;

namespace FolderSync
{
    /// <summary>
    /// Interaction logic for SyncPreview.xaml
    /// </summary>
    public partial class SyncPreview : Window
    {
        public List<SyncOperation> SyncOperations { get; set; }

        public SyncPreview(List<SyncOperation> syncOperations) {
            InitializeComponent();

            SyncOperations = syncOperations;
            DataContext = this;
        }


    }
}
