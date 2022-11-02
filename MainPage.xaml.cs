using ExtendedStorage;
using FEApp.Controls.File_Explorer_View;
using FEApp.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FEApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            SearchBar.PlaceholderText = "Search".GetLocalized();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            AddTab(UserDataPaths.GetForUser(App.user).Profile);
        }

        /// <summary>
        /// Navigate to the path entered in the path box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentPathBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                EStorageFolder folderToNavigate = EStorageFolder.GetFromPath(CurrentPathBox.Text);
                if (folderToNavigate != null)
                {
                    ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).Navigate(folderToNavigate.Path);
                }
                else
                {
                    CurrentPathBox.Text = ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).Source;
                }
            }
        }

        /// <summary>
        /// When the search query is submitted (Enter).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SearchBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).FilterByName(sender.Text);
        }

        private void FilesTabView_AddTabButtonClick(TabView sender, object args)
        {
            AddTab(UserDataPaths.GetForUser(App.user).Profile);
        }

        private void AddTab(string path, bool isClosable = true)
        {
            TabViewItem newTab = new TabViewItem
            {
                IsClosable = isClosable
            };

            FileExplorerView fileExplorer = new FileExplorerView();
            fileExplorer.Navigate(path);

            newTab.Content = fileExplorer;
            newTab.Header = Path.GetFileName(path);
            newTab.IconSource = new ImageIconSource { ImageSource = new BitmapImage(new Uri("ms-appx:///Data/Icons/folder-icon.png")) };
            CurrentPathBox.Text = path;

            FilesTabView.TabItems.Add(newTab);
            FilesTabView.SelectedItem = newTab;

            fileExplorer.OnFileOpenRequest += FileExplorer_OnFileOpenRequest;
            fileExplorer.OnSelectionChanged += FileExplorer_OnSelectionChanged;
            fileExplorer.OnNavigationComplete += FileExplorer_OnNavigationComplete;
            fileExplorer.OnMultiselectionStarted += FileExplorer_OnMultiselectionStarted;
            fileExplorer.OnMultiselectionCompleted += FileExplorer_OnMultiselectionCompleted;
        }

        private void FileExplorer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tab view tab content is the file explorer
        }

        private void FileExplorer_OnMultiselectionCompleted(FileExplorerView sender, FileNavigationArgs args)
        {
            MultiselectionButton.Background = (SolidColorBrush)Resources["MultiselectionButtonDisabled"];
        }

        private void FileExplorer_OnMultiselectionStarted(FileExplorerView sender, FileNavigationArgs args)
        {
            MultiselectionButton.Background = (SolidColorBrush)Resources["MultiselectionButtonEnabled"];
        }

        private void FileExplorer_OnNavigationComplete(FileExplorerView sender, FileNavigationArgs args)
        {
            if ((FilesTabView.SelectedItem as TabViewItem) != null)
            {
                (FilesTabView.SelectedItem as TabViewItem).Header = args.PathDisplayName;
                CurrentPathBox.Text = args.Path;
            }
        }

        private async void FileExplorer_OnFileOpenRequest(FileExplorerView sender, FileItemClickedArgs args)
        {
            if (args.ClickedItem.Type == FileItem.FileType.Folder || args.ClickedItem.Type == FileItem.FileType.Drive)
            {
                if (sender.CanGetItems)
                {
                    sender.Navigate(args.ClickedItem.Path);
                }
            }
            else if (args.ClickedItem.Type == FileItem.FileType.File)
            {
                try
                {
                    StorageFile StorageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(args.ClickedItem.Path);
                    await Launcher.LaunchFileAsync(StorageFile);
                }
                catch { }
            }
        }

        private void FilesTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);

            // Handle no tab opened
            if (sender.TabItems.Count == 0)
            {
                AddTab(UserDataPaths.GetForUser(App.user).Profile);
            }
        }

        private void GoBackButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).GoBackCommand.Execute(new object());
        }

        private void RefreshButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).RefreshCommand.Execute(new object());
        }

        private void CurrentPathBox_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void MultiselectionButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).MultiSelectCommand.Execute(new object());
        }

        private async void DrivesButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).NavigateToDrives();
        }

        private void UserFolderButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).Navigate(UserDataPaths.GetForUser(App.user).Profile);
        }

        private void FilesTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesTabView.SelectedIndex == -1) return;
            
            (FilesTabView.SelectedItem as TabViewItem).Header = ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).PathName;
            CurrentPathBox.Text = ((FilesTabView.SelectedItem as TabViewItem).Content as FileExplorerView).Source;
        }
    }
}
