using ExtendedStorage;
using ExtendedStorage.Archive;
using ExtendedStorage.Utils;
using FEApp.Enums;
using FEApp.Models;
using FEApp.Services;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FileAttributes = System.IO.FileAttributes;

namespace FEApp.Controls.File_Explorer_View
{
    public sealed partial class FileExplorerView : Page
    {
        // Lists
        private readonly IList<FileItem> FileItems = new List<FileItem>(); // List that contains all items
        private readonly ObservableCollection<FileItem> FileItemsFiltered = new ObservableCollection<FileItem>(); // List of filtered items, main shown list
        private readonly List<FileItem> CopiedFiles = new List<FileItem>(); // List of files/folders that are copied by the user
        public List<FileItem> SelectedItems = new List<FileItem>();

        public ICommand MultiSelectCommand { get; set; }
        public ICommand GoBackCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand PasteCommand { get; set; }
        public ICommand MakeArchiveCommand { get; set; }
        public ICommand ExtractArchiveCommand { get; set; }
        public ICommand CopyPathCommand { get; set; }
        public ICommand RenameCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand CreateNewFileCommand { get; set; }
        public ICommand CreateNewFolderCommand { get; set; }

        // PROPERTIES
        public string PathName { get; set; }
        public string Source { get; set; }
        public bool IsMultiselectionEnabled { get; set; }
        public bool CanGetItems { get; set; }

        // EVENTS
        public delegate void SelectionChangedHandler(object sender, SelectionChangedEventArgs e);
        public event SelectionChangedHandler OnSelectionChanged;

        public delegate void OnNavigationStartHandler(FileExplorerView sender, FileNavigationArgs args);
        public event OnNavigationStartHandler OnNavigationStart;

        public delegate void OnNavigationCompleteHandler(FileExplorerView sender, FileNavigationArgs args);
        public event OnNavigationCompleteHandler OnNavigationComplete;

        public delegate void OnNavigationFailedHandler(FileExplorerView sender, FileNavigationArgs args, Exception error);
        public event OnNavigationFailedHandler OnNavigationFail;

        public delegate void OnFileOpenRequestHandler(FileExplorerView sender, FileItemClickedArgs args);
        public event OnFileOpenRequestHandler OnFileOpenRequest;

        public delegate void OnDirectoryChangeHandler(FileExplorerView sender, DirectoryTreeModificationArgs args);
        public event OnDirectoryChangeHandler OnDirectoryChange;

        public delegate void OnMultiselectionStartHandler(FileExplorerView sender, FileNavigationArgs args);
        public event OnMultiselectionStartHandler OnMultiselectionStarted;

        public delegate void OnMultiselectionEndHandler(FileExplorerView sender, FileNavigationArgs args);
        public event OnMultiselectionStartHandler OnMultiselectionCompleted;

        public FileExplorerView()
        {
            InitializeComponent();

            // Assign commands
            MultiSelectCommand = new RelayCommand(MultiSelect, CanMultiSelect);
            GoBackCommand = new RelayCommand(GoBack, CanGoBack);
            RefreshCommand = new RelayCommand(Refresh, CanRefresh);
            CopyPathCommand = new RelayCommand(CopyLocation, CanCopyLocation);
            MakeArchiveCommand = new RelayCommand(MakeArchive, CanMakeArchive);
            ExtractArchiveCommand = new RelayCommand(ExtractArchive, CanExtractArchive);
            CopyCommand = new RelayCommand(Copy, CanCopy);
            PasteCommand = new RelayCommand(Paste, CanPaste);
            DeleteCommand = new RelayCommand(Delete, CanDelete);
            RenameCommand = new RelayCommand(Rename, CanRename);
            CreateNewFileCommand = new RelayCommand(CreateNewFile, CanCreateNewFile);
            CreateNewFolderCommand = new RelayCommand(CreateNewFolder, CanCreateNewFolder);

            // Localization single item
            MakeArchiveFlyoutItem.Text = "MakeArchive".GetLocalized();
            ExtractArchiveFlyoutItem.Text = "ExtractArchive".GetLocalized();
            CopyFlyoutItem.Text = "Copy".GetLocalized();
            CopyLocationFlyoutItem.Text = "CopyLocation".GetLocalized();
            DeleteFlyoutItem.Text = "Delete".GetLocalized();
            RenameFlyoutItem.Text = "Rename".GetLocalized();

            // Localization Adaptive Grid View right click
            RefreshFlyoutItem.Text = "Refresh".GetLocalized();
            PasteFlyoutItem.Text = "Paste".GetLocalized();
            CreateFlyoutItem.Text = "New".GetLocalized();
            FileFlyoutItem.Text = "File".GetLocalized();
            FolderFlyoutItem.Text = "Folder".GetLocalized();

            // Localization other
            EmptyFolderText.Text = "EmptyFolder".GetLocalized();

            FilesGrid.ItemsSource = FileItemsFiltered;
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((e.Parameter as string) == null)
            {
                Navigate(UserDataPaths.GetForUser(App.user).Profile);
            }
            else
            {
                Navigate(e.Parameter as string);
            }
        }

        public void FilterByName(string query)
        {
            List<FileItem> TempFiltered;

            TempFiltered = FileItems.Where(ccItem => ccItem.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();

            for (int i = FileItemsFiltered.Count - 1; i >= 0; i--)
            {
                FileItem item = FileItemsFiltered[i];
                if (!TempFiltered.Contains(item))
                {
                    FileItemsFiltered.Remove(item);
                }
            }

            foreach (FileItem item in TempFiltered)
            {
                if (!FileItemsFiltered.Contains(item))
                {
                    FileItemsFiltered.Add(item);
                }
            }
        }

        /// <summary>
        /// Get the actual directory content.
        /// </summary>
        /// <param name="directorySource">The directory to search in.</param>
        /// <returns></returns>
        public async void Navigate(string directorySource)
        {
            HandleNavigationStart(new FileNavigationArgs(directorySource));
            CanGetItems = false;
            FileItemsFiltered.Clear();
            FileItems.Clear();

            try
            {
                if (directorySource != null)
                {
                    EStorageFolder source = EStorageFolder.GetFromPath(directorySource);
                    Source = directorySource;
                    PathName = source.Name;

                    foreach (EStorageFolder folder in source.GetFolders())
                    {
                        if (folder == null)
                        {
                            continue;
                        }

                        float opacity;
                        if (folder.Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            opacity = 0.70f;
                        }
                        else
                        {
                            opacity = 1;
                        }

                        FileItems.Add(new FileItem(await EStorageFile.GetFromApplicationFolder("/Data/Icons/folder-icon.png").TryGetImage(), opacity, string.Empty, FileItem.FileType.Folder, folder.Name, folder.Path));
                    }
                    foreach (EStorageFile file in source.GetFiles())
                    {
                        if (file == null)
                        {
                            continue;
                        }

                        float opacity;
                        if (file.Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            opacity = 0.70f;
                        }
                        else
                        {
                            opacity = 1;
                        }

                        FileItems.Add(new FileItem(await EStorageFile.GetFromApplicationFolder("/Data/Icons/file-icon.png").TryGetImage(), opacity, file.FileType, FileItem.FileType.File, file.Name, file.Path));
                    }

                    // Handle no files on the folder.
                    if (FileItems.Count == 0)
                    {
                        EmptyFolderText.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    else
                    {
                        EmptyFolderText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }

                    HandleNavigationComplete(new FileNavigationArgs(directorySource));
                }
                else
                {
                    HandleNavigationFail(new FileNavigationArgs(directorySource), new Exception("NavigationPathInvalid"));
                }
            }
            catch (Exception ex)
            {
                HandleNavigationFail(new FileNavigationArgs(directorySource), ex);
            }
            FileItems.ToList().ForEach(FileItemsFiltered.Add);
            CanGetItems = true;
        }

        /// <summary>
        /// Checks if the user can multiselect items.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user can either multiselect items or not.</returns>
        private bool CanMultiSelect(object value)
        {
            return true;
        }

        private void MultiSelect(object value)
        {
            if (IsMultiselectionEnabled == false)
            {
                FilesGrid.SelectionMode = ListViewSelectionMode.Multiple;
                FilesGrid.IsMultiSelectCheckBoxEnabled = true;
                FilesGrid.IsItemClickEnabled = false;
                IsMultiselectionEnabled = true;
                HandleMultiselectionStarted(new FileNavigationArgs(Source));
            }
            else
            {
                FilesGrid.SelectionMode = ListViewSelectionMode.Single;
                FilesGrid.IsMultiSelectCheckBoxEnabled = false;
                FilesGrid.IsItemClickEnabled = true;
                IsMultiselectionEnabled = false;
                HandleMultiselectionCompleted(new FileNavigationArgs(Source));
            }
        }

        /// <summary>
        /// Checks if the user can go back.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to go back or not.</returns>
        private bool CanGoBack(object value)
        {
            try
            {
                if (Source != "ExternalStorageDevices".GetLocalized())
                {
                    return Directory.GetParent(Source) != null;
                }
                else 
                    return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Go to the parent folder of the actual one.
        /// </summary>
        /// <param name="value"></param>
        private void GoBack(object value)
        {
            try
            {
                if (!CanGetItems)
                {
                    return;
                }

                DirectoryInfo parent = Directory.GetParent(Source);
                if (parent != null)
                {
                    Navigate(parent.ToString()); // Navigates to the parent directory
                }
                else 
                {
                    NavigateToDrives();
                }

            }
            catch (Exception ex)
            {
                NavigateToDrives();
                //HandleNavigationFail(new FileNavigationArgs(Source), ex);
            }
        }

        /// <summary>
        /// Checks if user can refresh the actual page.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to refresh the actual page or not.</returns>
        private bool CanRefresh(object value)
        {
            return true; // Always possible
        }

        /// <summary>
        /// Refresh the current folder's frame.
        /// </summary>
        /// <param name="value"></param>
        private void Refresh(object value)
        {
            if (!CanGetItems)
            {
                return;
            }
            Navigate(Source); // Get again all items, no refresh frame.
        }

        /// <summary>
        /// Displays a list of plugged devices.
        /// </summary>
        /// <param name="value"></param>
        public async Task NavigateToDrives()
        {
            if (CanGetItems)
            {
                FileItems.Clear();
                FileItemsFiltered.Clear();

                IReadOnlyList<string> removabledevices = await StorageUtils.GetRemovableDrives();
                List<string> devices = new List<string>(removabledevices);
                devices.AddRange(await StorageUtils.GetPermanentDrives());
                devices.Sort();

                if (devices.Count > 0)
                {
                    Source = "ExternalStorageDevices".GetLocalized(); // Avoid NullReferenceException when going back
                }

                foreach (string device in devices)
                {
                    if (removabledevices.Contains(device))
                    {
                        FileItems.Add(new FileItem(await EStorageFile.GetFromApplicationFolder("/Data/Icons/usb-drive.png").TryGetImage(), 1, string.Empty, FileItem.FileType.Drive, device, device));
                    }
                    else
                    {
                        FileItems.Add(new FileItem(await EStorageFile.GetFromApplicationFolder("/Data/Icons/hdd.png").TryGetImage(), 1, string.Empty, FileItem.FileType.Drive, device, device));
                    }
                }
                FileItems.ToList().ForEach(FileItemsFiltered.Add);
            }
        }

        /// <summary>
        /// Checks if the user can rename the selected item.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to rename the selected item or not.</returns>
        private bool CanRename(object value)
        {
            return FilesGrid.SelectedItems.Count == 1 && (FilesGrid.SelectedItems[0] as FileItem) != null && FilesGrid.SelectedItems.Any(item => (item as FileItem).Type != FileItem.FileType.Drive); // Can only rename one item
        }

        /// <summary>
        /// Rename the actual selected item.
        /// </summary>
        /// <param name="value"></param>
        private async void Rename(object value)
        {
            // Handle file to rename wasn't available.
            EStorageItem item = EStorageFile.GetFromPath((FilesGrid.SelectedItems[0] as FileItem).Path);
            if (item == null)
            {
                return;
            }

            // Handle user didn't give their consent to rename the file.
            FileEditCreateBox rn = new FileEditCreateBox(item.Name, "Rename".GetLocalized(), "Cancel".GetLocalized(), item.Name);
            if (await rn.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            // Handle user entered the same name (We don't need to rename).
            if (rn.Result == item.Name)
            {
                return;
            }

            // Rename the item and trigger change.
            item.Rename(rn.Result);
            if (CanGetItems)
            {
                Navigate(Source);

                if (item is EStorageFile)
                {
                    HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(item.Path, $"{item.Path}\\{rn.Result}", DirectoryChangeOperation.FileRenamed));
                }
                else if (item is EStorageFolder)
                {
                    HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(item.Path, $"{item.Path}\\{rn.Result}", DirectoryChangeOperation.DirectoryRenamed));
                }
            }
        }

        /// <summary>
        /// Checks if the user can make an archive.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user can either make an archive or not.</returns>
        private bool CanMakeArchive(object value)
        {
            if (FilesGrid.SelectedItems.Count == 1)
            {
                if (FilesGrid.SelectedItems.All(item => (item as FileItem).Type == FileItem.FileType.Folder))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Makes an archive from selected items.
        /// </summary>
        /// <param name="value"></param>
        private async void MakeArchive(object value)
        {
            EStorageFolder selectedFolder = EStorageFolder.GetFromPath((FilesGrid.SelectedItem as FileItem).Path);
            EStorageFolder currentPath = EStorageFolder.GetFromPath(Source);
            FileEditCreateBox newArchiveBox = new FileEditCreateBox("MakeArchive".GetLocalized(), "Create".GetLocalized(), "Cancel".GetLocalized(), "ArchiveName".GetLocalized());
            if (await newArchiveBox.ShowAsync() == ContentDialogResult.Primary)
            {
                OperationBox info = new OperationBox("MakeArchive".GetLocalized());
                info.Show();

                string archiveName;
                if (newArchiveBox.Result.EndsWith(".zip"))
                {
                    archiveName = newArchiveBox.Result;
                }
                else
                {
                    archiveName = newArchiveBox.Result + ".zip";
                }

                ArchiveCreator archiveCreator = new ArchiveCreator();
                EStorageFile archiveFile = currentPath.CreateFile(archiveName, ExtendedStorage.Enums.FileAlreadyExists.GenerateUniqueName);
                await archiveCreator.CreateArchiveFromDirectory(selectedFolder, archiveFile);

                if (CanGetItems)
                {
                    Navigate(Source);
                }
                info.Close();
                HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(Source, archiveFile.Path, DirectoryChangeOperation.NewFileAdded));
            }
        }

        /// <summary>
        /// Checks if the user can extract an archive to the specified directory.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user can either extract an archive or not.</returns>
        private bool CanExtractArchive(object value)
        {
            return FilesGrid.SelectedItems.Count == 1 && (FilesGrid.SelectedItems[0] as FileItem).Extension == ".zip";
        }

        /// <summary>
        /// Extracts an archive to the specified directory.
        /// </summary>
        /// <param name="value"></param>
        private void ExtractArchive(object value)
        {
            OperationBox info = new OperationBox("ExtractArchive".GetLocalized());
            info.Show();

            FileItem item = FilesGrid.SelectedItem as FileItem;

            EStorageFile archive = EStorageFile.GetFromPath(item.Path);
            EStorageFolder destination = EStorageFolder.GetFromPath(Source);

            if (archive != null && destination != null)
            {
                EStorageFolder extractedArchivePath = destination.CreateFolder(archive.Name, ExtendedStorage.Enums.FileAlreadyExists.GenerateUniqueName);
                ArchiveExtractor.ExtractArchive(archive, extractedArchivePath);
                HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(Source, extractedArchivePath.Path, DirectoryChangeOperation.NewDirectoryAdded));
            }

            if (CanGetItems)
            {
                Navigate(Source);
            }
            info.Close();
        }

        /// <summary>
        /// Checks if the user can copies the selected items.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to copy selected items or not.</returns>
        private bool CanCopy(object value)
        {
            return FilesGrid.SelectedItems.Count > 0 && FilesGrid.SelectedItems.Any(item => (item as FileItem).Type != FileItem.FileType.Drive); // More than one item selected and not a drive
        }

        /// <summary>
        /// Copies selected items.
        /// </summary>
        /// <param name="value"></param>
        private void Copy(object value)
        {
            CopiedFiles.Clear(); // Clear before copy
            FilesGrid.SelectedItems.ToList().ForEach(item => CopiedFiles.Add(item as FileItem));
        }

        /// <summary>
        /// Checks if the user can paste items.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to paste items or not. (True if the clipboard count > 0).</returns>
        private bool CanPaste(object value)
        {
            return CopiedFiles.Count > 0 && CopiedFiles.All(item => item.Type != FileItem.FileType.Drive); // More than one item was copied and the list of copied items doesn't contains any drive
        }

        /// <summary>
        /// Paste the actual item.
        /// </summary>
        /// <param name="value"></param>
        private void Paste(object value)
        {
            OperationBox info = new OperationBox("Paste".GetLocalized());
            info.Show();
            foreach (FileItem fileItem in CopiedFiles)
            {
                try
                {
                    EStorageFolder currentFolder = EStorageFolder.GetFromPath(Source);
                    if (currentFolder != null)
                    {
                        if (fileItem.Type == FileItem.FileType.File)
                        {
                            EStorageFile file = EStorageFile.GetFromPath(fileItem.Path);
                            if (file != null)
                            {
                                file.CopyTo(currentFolder, file.Name);
                                HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(file.Path, $"{currentFolder}\\{file.Name}", DirectoryChangeOperation.FilePasted));
                            }
                        }
                        else if (fileItem.Type == FileItem.FileType.Folder)
                        {
                            EStorageFolder folder = EStorageFolder.GetFromPath(fileItem.Path);
                            if (folder != null)
                            {
                                folder.CopyTo(currentFolder, folder.Name);
                                HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(folder.Path, $"{currentFolder}\\{folder.Name}", DirectoryChangeOperation.DirectoryPasted));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error occured: {ex.Message}, Win32 Error code: {Marshal.GetLastWin32Error()}");
                }
            }
            CopiedFiles.Clear(); // Clean the list to make sure that the user will not paste twice the same
            FilesGrid.SelectedItem = null;

            if (CanGetItems)
            {
                Navigate(Source);
            }
            info.Close();
        }

        /// <summary>
        /// Checks if the user can delete the actual item.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to delete the actual item or not.</returns>
        private bool CanDelete(object value)
        {
            return FilesGrid.SelectedItems.Count > 0 && FilesGrid.SelectedItems.Any(item => (item as FileItem).Type != FileItem.FileType.Drive); // More than one item selected and not a drive
        }

        /// <summary>
        /// Delete the actual item.
        /// </summary>
        /// <param name="value"></param>
        private async void Delete(object value)
        {
            // Handle user didn't give their consent to delete the file(s)/folder(s).
            if (await DialogBoxService.ShowContentBox("Delete".GetLocalized(), "DeleteFile".GetLocalized(), "Yes".GetLocalized(), "No".GetLocalized()) != ContentDialogResult.Primary)
            {
                return;
            }

            // Delete file or folder and raise the event.
            OperationBox info = new OperationBox("Delete".GetLocalized());
            info.Show();
            foreach (FileItem item in FilesGrid.SelectedItems.Cast<FileItem>())
            {
                if (item.Type == FileItem.FileType.File)
                {
                    EStorageFile file = EStorageFile.GetFromPath(item.Path);
                    if (file != null)
                    {
                        file.Delete();
                        HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(file.Path, Source, DirectoryChangeOperation.FileDeleted));
                    }
                }
                else if (item.Type == FileItem.FileType.Folder)
                {
                    EStorageFolder folder = EStorageFolder.GetFromPath(item.Path);
                    if (folder != null)
                    {
                        folder.Delete();
                        HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(folder.Path, Source, DirectoryChangeOperation.DirectoryDeleted));
                    }
                }
            }
            info.Close();

            // Refresh.
            if (CanGetItems)
            {
                Navigate(Source);
            }
        }

        /// <summary>
        /// Check if the user can copy the actual item location.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user has either the capacity to copy the item location or not.</returns>
        private bool CanCopyLocation(object value)
        {
            return FilesGrid.SelectedItems.Count > 0 && FilesGrid.SelectedItems.Any(item => (item as FileItem).Type != FileItem.FileType.Drive); // More than one item selected and not a drive
        }

        /// <summary>
        /// Copy the actual item location.
        /// </summary>
        /// <param name="value"></param>
        private void CopyLocation(object value)
        {
            DataPackage dp = new DataPackage();
            dp.SetText((FilesGrid.SelectedItems[0] as FileItem).Path);
            dp.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(dp);
            try
            {
                Clipboard.Flush(); // Keep the path copied even if the app is closed
            }
            catch { }
        }

        /// <summary>
        /// Checks if the user can create a new file.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user can either create a new file or not.</returns>
        private bool CanCreateNewFile(object value)
        {
            return true;
        }

        /// <summary>
        /// Creates a new file at the actual location.
        /// </summary>
        /// <param name="value"></param>
        private async void CreateNewFile(object value)
        {
            // Handle current folder not found.
            EStorageFolder actualFolder = EStorageFolder.GetFromPath(Source);
            if (actualFolder == null)
            {
                return;
            }

            // Handle user didn't give their consent to create a new file.
            FileEditCreateBox newFileBox = new FileEditCreateBox("CreateNewFile".GetLocalized(), "Create".GetLocalized(), "Cancel".GetLocalized(), "FileName".GetLocalized());
            if (await newFileBox.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            // Create a new file and raise the event.
            OperationBox info = new OperationBox("CreateNewFile".GetLocalized());
            info.Show();
            EStorageFile newFile = actualFolder.CreateFile(newFileBox.Result);
            info.Close();
            HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(newFile.Path, newFile.Path, DirectoryChangeOperation.NewFileAdded));

            // Refresh.
            if (CanGetItems)
            {
                Navigate(Source);
            }
        }

        /// <summary>
        /// Checks if the user can create a new folder.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns if the user can either create a new folder or not.</returns>
        private bool CanCreateNewFolder(object value)
        {
            return true;
        }

        /// <summary>
        /// Creates a new folder at the actual location.
        /// </summary>
        /// <param name="value"></param>
        private async void CreateNewFolder(object value)
        {
            // Handle current folder wasn't found.
            EStorageFolder actualFolder = EStorageFolder.GetFromPath(Source);
            if (actualFolder == null)
            {
                return;
            }

            // Handle user didn't give their consent to create a new folder.
            FileEditCreateBox newFileBox = new FileEditCreateBox("CreateNewFolder".GetLocalized(), "Create".GetLocalized(), "Cancel".GetLocalized(), "FolderName".GetLocalized());
            if (await newFileBox.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            // Create a new folder and raise the event.
            OperationBox info = new OperationBox("CreateNewFolder".GetLocalized());
            info.Show();
            EStorageFolder newFolder = actualFolder.CreateFolder(newFileBox.Result);
            info.Close();
            HandleDirectoryTreeChange(new DirectoryTreeModificationArgs(newFolder.Path, newFolder.Path, DirectoryChangeOperation.NewDirectoryAdded));

            // Refresh.
            if (CanGetItems)
            {
                Navigate(Source);
            }
        }

        private void FileItemFlyout_Opening(object sender, object e)
        {
            MenuFlyout flyout = sender as MenuFlyout;

            // Select right clicked item
            if (flyout.Target is GridViewItem gridItem)
            {
                if (!gridItem.IsSelected)
                {
                    FilesGrid.SelectedItem = null;
                    gridItem.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// When a single click is fired on an item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FileItem item)
            {
                if (item.Type == FileItem.FileType.Folder || item.Type == FileItem.FileType.Drive)
                {
                    if (CanGetItems)
                    {
                        Navigate(item.Path);
                    }
                }
                else if (item.Type == FileItem.FileType.File)
                {
                    HandleFileOpenRequest(new FileItemClickedArgs(item));
                }
            }
        }

        // Event handlers
        private void HandleNavigationStart(FileNavigationArgs args)
        {
            if (OnNavigationStart == null)
            {
                return;
            }
            OnNavigationStart(this, args);
        }

        private void HandleNavigationComplete(FileNavigationArgs args)
        {
            if (OnNavigationComplete == null)
            {
                return;
            }
            OnNavigationComplete(this, args);
        }

        private void HandleNavigationFail(FileNavigationArgs args, Exception error)
        {
            if (OnNavigationFail == null)
            {
                return;
            }
            OnNavigationFail(this, args, error);
        }

        private void HandleFileOpenRequest(FileItemClickedArgs args)
        {
            if (OnFileOpenRequest == null)
            {
                return;
            }
            OnFileOpenRequest(this, args);
        }

        private void HandleDirectoryTreeChange(DirectoryTreeModificationArgs args)
        {
            if (OnDirectoryChange == null)
            {
                return;
            }
            OnDirectoryChange(this, args);
        }

        private void HandleMultiselectionStarted(FileNavigationArgs args)
        {
            if (OnMultiselectionStarted == null)
            {
                return;
            }
            OnMultiselectionStarted(this, args);
        }

        private void HandleMultiselectionCompleted(FileNavigationArgs args)
        {
            if (OnMultiselectionCompleted == null)
            {
                return;
            }
            OnMultiselectionCompleted(this, args);
        }

        private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OnSelectionChanged == null)
            {
                return;
            }
            OnSelectionChanged(sender, e);
        }

        private void FilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<FileItem> tempList = new List<FileItem>();
            foreach (FileItem item in (sender as AdaptiveGridView).SelectedItems.Cast<FileItem>())
            {
                tempList.Add(item);
            }
            SelectedItems = tempList;
            HandleSelectionChanged(sender, e);
        }
    }
}
