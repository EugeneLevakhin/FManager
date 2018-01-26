using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Drawing;
using System.Windows.Interop;
using System.Threading;

namespace FManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: clip time of loading ico images
        // TODO: display progress bar during copying
        // TODO: Commands
        // TODO: searching files Multithreading
        // TODO: Context Menu
        // TODO: replace folders

        List<string> foundFolders = new List<string>();
        List<string> foundFiles = new List<string>();

        int numberOfAvailableThreads;
        int numberOfIOThreads;          // not use in this programm

        private object lockObj = new object();

        public MainWindow()
        {
            InitializeComponent();

            Left = Properties.Settings.Default.WindowPosition.Left;
            Top = Properties.Settings.Default.WindowPosition.Top;
            Width = Properties.Settings.Default.WindowPosition.Width;
            Height = Properties.Settings.Default.WindowPosition.Height;

            EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, new RoutedEventHandler(EventBasedMouseLeftButtonHandler));   // for ListBoxItem, because native event MouseDown don't rise 

            leftList.Tag = Properties.Settings.Default.leftListPath;
            rightList.Tag = Properties.Settings.Default.rightListPath;

            try
            {
                GoToCurrentFolder(leftList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                InRootOfFileSystem(leftList);
            }

            try
            {
                GoToCurrentFolder(rightList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                InRootOfFileSystem(rightList); ;
            }

            ThreadPool.GetAvailableThreads(out numberOfAvailableThreads, out numberOfIOThreads);
        }

        private void InRootOfFileSystem(ListBox list)                                              // go to list of drives
        {
            list.Items.Clear();

            list.Tag = string.Empty;
            if (list.Name == "leftList")
            {
                txtLeftPath.Text = string.Empty;
            }
            else
            {
                txtRightPath.Text = string.Empty;
            }

            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Unknown && drive.DriveType != DriveType.Ram && drive.DriveType != DriveType.Network && drive.IsReady)
                {
                    AddListBoxItem(drive, list);
                }
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem senderListBoxItem = sender as ListBoxItem;
            ListBox parrentOfSender = senderListBoxItem.Parent as ListBox;

            Grid tempGrid = senderListBoxItem.Content as Grid;

            string currentFolderOfActiveList = leftList.Tag.ToString();

            if (parrentOfSender.Name == "rightList")
            {
                currentFolderOfActiveList = rightList.Tag.ToString();
            }

            string fullNameOfFileSystemItem = senderListBoxItem.Tag.ToString();

            if (Directory.Exists(fullNameOfFileSystemItem))
            {
                currentFolderOfActiveList = fullNameOfFileSystemItem;
                parrentOfSender.Tag = currentFolderOfActiveList;
                GoToCurrentFolder(parrentOfSender);
            }
            else if (File.Exists(fullNameOfFileSystemItem))                                                              // open file
            {
                System.Diagnostics.Process.Start(fullNameOfFileSystemItem);
            }
        }

        private void GoToCurrentFolder(ListBox list)
        {
            lock (lockObj)
            {
                if (list.Tag.ToString() != string.Empty)
                {
                    string currentFolderOfActiveList = list.Tag.ToString();

                    DirectoryInfo directoryInfo = new DirectoryInfo(currentFolderOfActiveList);
                    try                                                                             // if directory require admin permission
                    {
                        DirectoryInfo[] directories = directoryInfo.GetDirectories();

                        list.Items.Clear();

                        foreach (var directory in directories)
                        {
                            if (directory.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.NotContentIndexed)
                                || directory.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory)
                                || directory.Attributes.Equals(FileAttributes.Hidden | FileAttributes.Directory)
                                || directory.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory | FileAttributes.NotContentIndexed)
                                )
                            {
                                continue;
                            }
                            AddListBoxItem(directory, list);
                        }

                        FileInfo[] files = directoryInfo.GetFiles();
                        foreach (var file in files)
                        {
                            AddListBoxItem(file, list);
                        }

                        if (list.Name == "leftList")
                        {
                            txtLeftPath.Text = currentFolderOfActiveList;
                        }
                        else
                        {
                            txtRightPath.Text = currentFolderOfActiveList;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        if (list.Name == "leftList")
                        {
                            btnUp_Click(btnLeftUp, null);
                        }
                        else
                        {
                            btnUp_Click(btnRightUp, null);
                        }
                    }
                }
                else
                {
                    InRootOfFileSystem(list);
                }
            }
        }

        private void AddListBoxItem(object fileSystemItem, ListBox list)
        {
            ListBoxItem listBoxItem = new ListBoxItem();
            listBoxItem.HorizontalAlignment = HorizontalAlignment.Stretch;
            BitmapImage tempBitmap;
            string nameOfFileSystemItem = string.Empty;

            System.Windows.Controls.Image image = new System.Windows.Controls.Image();

            if (fileSystemItem is DriveInfo)
            {
                DriveInfo drive = fileSystemItem as DriveInfo;
                nameOfFileSystemItem = drive.Name;
                listBoxItem.Tag = drive.Name;

                if (drive.DriveType == DriveType.CDRom)
                {
                    tempBitmap = new BitmapImage(new Uri("Resources/disc.png", UriKind.Relative));
                }
                else
                {
                    tempBitmap = new BitmapImage(new Uri("Resources/hardDrive.png", UriKind.Relative));
                }
                image.Source = tempBitmap;
            }
            else if (fileSystemItem is FileSystemInfo)
            {
                listBoxItem.Tag = (fileSystemItem as FileSystemInfo).FullName;
                nameOfFileSystemItem = (fileSystemItem as FileSystemInfo).Name;

                if (fileSystemItem is DirectoryInfo)
                {
                    tempBitmap = new BitmapImage(new Uri("Resources/folder.jpg", UriKind.Relative));
                    image.Source = tempBitmap;
                }

                else if (fileSystemItem is FileInfo)
                {
                    Icon icon;
                    FileInfo fi = fileSystemItem as FileInfo;
                    icon = System.Drawing.Icon.ExtractAssociatedIcon(fi.FullName);
                    image.Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }

            image.Width = 20;
            image.Height = 20;

            Grid grid = new Grid();

            ColumnDefinition cm = new ColumnDefinition();
            cm.Width = new GridLength(1, GridUnitType.Star);

            ColumnDefinition cm2 = new ColumnDefinition();
            cm2.Width = new GridLength(4, GridUnitType.Star);

            grid.ColumnDefinitions.Add(cm);
            grid.ColumnDefinitions.Add(cm2);

            Grid.SetColumn(image, 0);
            grid.Children.Add(image);

            Label label = new Label();
            label.Content = nameOfFileSystemItem;
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            listBoxItem.Content = grid;

            listBoxItem.MouseDoubleClick += ListBoxItem_MouseDoubleClick;
            listBoxItem.MouseDown += EventBasedMouseLeftButtonHandler;
            listBoxItem.KeyDown += ListBoxItem_KeyDown;
            if (!(fileSystemItem is FileInfo))
            {
                listBoxItem.AllowDrop = true;
                listBoxItem.Drop += List_Drop;
            }

            if (!(fileSystemItem is DriveInfo))
            {
                AddContextMenuToItem(listBoxItem);
            }

            list.Items.Add(listBoxItem);
        }

        private void ListBoxItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ListBoxItem_MouseDoubleClick(sender, null);
            }
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            string currentFolderOfActiveList = leftList.Tag.ToString();
            ListBox tempList = leftList;

            if ((sender as Button).Name == "btnRightUp")
            {
                currentFolderOfActiveList = rightList.Tag.ToString();
                tempList = rightList;
            }

            if (currentFolderOfActiveList.Length > 3)                                     // deleting last part of path and goto up folder
            {
                int i = currentFolderOfActiveList.Length - 1;
                while (!currentFolderOfActiveList[i].Equals('\\'))
                {
                    i--;
                }
                if (i == 2)
                {
                    i++;
                }
                currentFolderOfActiveList = currentFolderOfActiveList.Substring(0, i);
                tempList.Tag = currentFolderOfActiveList;
                GoToCurrentFolder(tempList);
            }
            else
            {
                InRootOfFileSystem(tempList);
            }
        }

        private void CommandClose_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CommandCopy_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CommandPaste_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void txtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (sender as TextBox).Text != string.Empty)
            {
                ListBox listBox = leftList;
                string previosPath = string.Empty;
                string changedPath = txtLeftPath.Text;

                if ((sender as TextBox).Name == "txtRightPath")
                {
                    listBox = rightList;
                    changedPath = txtRightPath.Text;
                }

                previosPath = listBox.Tag.ToString();

                if (changedPath.Length == 1)
                {
                    changedPath += ":\\";
                }
                listBox.Tag = changedPath;
                try
                {
                    GoToCurrentFolder(listBox);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    listBox.Tag = previosPath;
                    GoToCurrentFolder(listBox);
                }
            }
        }

        private void EventBasedMouseLeftButtonHandler(object sender, RoutedEventArgs e)
        {
            ListBoxItem senderListBoxItem = sender as ListBoxItem;
            ListBox sourceList = senderListBoxItem.Parent as ListBox;

            senderListBoxItem.AllowDrop = false;

            List<ListBoxItem> listOfSelectedItems = new List<ListBoxItem>();
            listOfSelectedItems = sourceList.SelectedItems.OfType<ListBoxItem>().ToList<ListBoxItem>();
            if (listOfSelectedItems.Count == 0)
            {
                listOfSelectedItems.Add(senderListBoxItem);
            }

            DragDrop.DoDragDrop(sourceList, listOfSelectedItems, DragDropEffects.Move);
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            List<ListBoxItem> movedItems = e.Data.GetData(typeof(List<ListBoxItem>)) as List<ListBoxItem>;

            ListBox sourceList = movedItems[0].Parent as ListBox;
            ListBox destinationList = null;
            string destinationPath = string.Empty;

            if (sender is ListBox)                                                                  // sender - receiver of drop
            {
                destinationList = sender as ListBox;
                destinationPath = destinationList.Tag.ToString();
            }
            else if (sender is ListBoxItem)
            {
                ListBoxItem destItem = sender as ListBoxItem;
                destinationPath = destItem.Tag.ToString();

                destinationList = destItem.Parent as ListBox;
            }

            if (sourceList.Tag.ToString().Length < 3 || destinationPath.Length < 3)     // if moved item is drive (or move in list of drives) - disable drop 
            {
                return;
            }

            foreach (var item in movedItems)
            {
                string fullNameOfMovedFileSystemItem = item.Tag.ToString();
                string copyOfMovedFileSystemItem = System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(item.Tag.ToString()));
           
                if (Directory.Exists(fullNameOfMovedFileSystemItem))                                     // if folder
                {
                    if (System.IO.Path.GetPathRoot(fullNameOfMovedFileSystemItem) != System.IO.Path.GetPathRoot(copyOfMovedFileSystemItem))   // if drives is different
                    {
                        Task task = CopyFoldersAsync(fullNameOfMovedFileSystemItem, copyOfMovedFileSystemItem);
                        e.Handled = true;
                        task.ContinueWith(t => this.Dispatcher.Invoke(() =>
                        {
                            GoToCurrentFolder(sourceList);
                            GoToCurrentFolder(destinationList);
                            txtStatus.Text = "";
                        }));
                    }
                    else
                    {
                        DirectoryInfo directory = new DirectoryInfo(fullNameOfMovedFileSystemItem);
                        directory.MoveTo(copyOfMovedFileSystemItem);
                        sourceList.Items.Remove(item);
                        if (sender is ListBox)
                        {
                            destinationList.Items.Add(item);
                        }
                        e.Handled = true;
                    }

                }
                else if (File.Exists(fullNameOfMovedFileSystemItem))                                      // if file
                {
                    FileInfo movedFile = new FileInfo(fullNameOfMovedFileSystemItem);

                    if (System.IO.Path.GetPathRoot(fullNameOfMovedFileSystemItem) == System.IO.Path.GetPathRoot(copyOfMovedFileSystemItem))   // if drives is a same
                    {
                        movedFile.MoveTo(copyOfMovedFileSystemItem);
                        sourceList.Items.Remove(item);
                        if (sender is ListBox)
                        {
                            destinationList.Items.Add(item);
                        }
                        e.Handled = true;
                    }
                    else
                    {
                        Task task = Task.Factory.StartNew(() =>
                        {

                            try
                            {
                                movedFile.CopyTo(copyOfMovedFileSystemItem);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                MessageBox.Show(ex.Message + " Try run programm as administrator");
                                e.Handled = true;
                                return;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                                e.Handled = true;
                                return;
                            }
                        });
                        task.ContinueWith(t => this.Dispatcher.Invoke(() =>
                        {
                            GoToCurrentFolder(sourceList);
                            GoToCurrentFolder(destinationList);
                        }));
                    }
                }
                item.Tag = copyOfMovedFileSystemItem;
            }
        }

        private void List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                if ((sender as ListBox).Name == "leftList")
                {
                    btnUp_Click(btnLeftUp, null);
                }
                else
                {
                    btnUp_Click(btnRightUp, null);
                }
            }
        }

        private void CopyFolders(object pairOfPath)
        {
            string targetFolder = (pairOfPath as PairOfPath).DestinationPath;

            DirectoryInfo sourceDir = new DirectoryInfo((pairOfPath as PairOfPath).SourcePath);
            DirectoryInfo directory = Directory.CreateDirectory(targetFolder);

            foreach (var file in sourceDir.GetFiles())
            {
                this.Dispatcher.Invoke(() => txtStatus.Text = "Copying " + file.Name);
                file.CopyTo(System.IO.Path.Combine(targetFolder, file.Name));
            }

            foreach (var dir in sourceDir.GetDirectories())
            {
                CopyFolders(new PairOfPath(dir.FullName, System.IO.Path.Combine(targetFolder, dir.Name)));
            }
        }

        private async Task CopyFoldersAsync(string sourceFolder, string targetFolder)
        {
            await Task.Factory.StartNew(CopyFolders, new PairOfPath(sourceFolder, targetFolder));
        }

        private void AddContextMenuToItem(ListBoxItem item)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem menuItemDelete = new MenuItem();
            menuItemDelete.Header = "Delete";
            menuItemDelete.Click += MenuItemDelete_Click;
            menu.Items.Add(menuItemDelete);
            item.ContextMenu = menu;
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            ListBox parrent = lbi.Parent as ListBox;
            Grid gr = lbi.Content as Grid;
            Label lb = gr.Children[1] as Label;

            string s = System.IO.Path.Combine(parrent.Tag.ToString(), lb.Content.ToString());

            if (Directory.Exists(s))
            {
                DirectoryInfo dir = new DirectoryInfo(s);
                dir.Delete(true);
            }
            else if (File.Exists(s))
            {
                FileInfo file = new FileInfo(s);
                file.Delete();
            }
            parrent.Items.Remove(lbi);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int numberOfAvailableThreads2;
            int numberOfIOThreads2;
            ThreadPool.GetAvailableThreads(out numberOfAvailableThreads2, out numberOfIOThreads2);
            if (numberOfAvailableThreads2 < numberOfAvailableThreads)
            {
                if (MessageBox.Show("Comlete all processes?", "Not all processes is comleted", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; ;
                }
            }

            Properties.Settings.Default.WindowPosition = this.RestoreBounds;

            Properties.Settings.Default.leftListPath = leftList.Tag.ToString();
            Properties.Settings.Default.rightListPath = rightList.Tag.ToString();

            Properties.Settings.Default.Save();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBoxSearchSender = sender as TextBox;
            if (e.Key == Key.Enter && textBoxSearchSender.Text != string.Empty)
            {
                ListBox activeListBox = leftList;
                TextBox activeTxtPath = txtLeftPath;

                if (textBoxSearchSender.Name == "txtSearchRight")
                {
                    activeListBox = rightList;
                    activeTxtPath = txtRightPath;
                }

                activeListBox.Items.Clear();
                txtStatus.Text = "searching...";
                activeTxtPath.Text = string.Empty;

                Task task = SearchFileItemsAsync(activeListBox.Tag.ToString(), textBoxSearchSender.Text);
                //e.Handled = true;
                task.ContinueWith(t => this.Dispatcher.Invoke(() =>
                {
                    foreach (var foundFolder in foundFolders)
                    {
                        DirectoryInfo directory = new DirectoryInfo(foundFolder);
                        AddListBoxItem(directory, activeListBox);
                    }
                    foreach (var foundFile in foundFiles)
                    {
                        FileInfo file = new FileInfo(foundFile);
                        AddListBoxItem(file, activeListBox);
                    }

                    foundFolders.Clear();
                    foundFiles.Clear();

                    txtStatus.Text = "";
                }));
            }
        }

        private void SearchFileItems(object pathAndSearchPattern)
        {
          string searchPattern = (pathAndSearchPattern as PathAndSearchPattern).SearchPattern.ToLower();
          string path = (pathAndSearchPattern as PathAndSearchPattern).Path.ToLower();

            if (path.Equals(""))                                                                            // if list of drives
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Unknown && drive.DriveType != DriveType.Ram && drive.DriveType != DriveType.Network && drive.IsReady)
                    {
                        SearchFileItems(new PathAndSearchPattern(drive.Name, searchPattern));
                    }
                }
            }
            else
            {
                try                                                                             // if directory require admin permission
                {
                    DirectoryInfo currentDirectory = new DirectoryInfo(path);

                    DirectoryInfo[] nestedDirectories = currentDirectory.GetDirectories();
                    foreach (var folder in nestedDirectories)
                    {
                        if (folder.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.NotContentIndexed)
                                    || folder.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory)
                                    || folder.Attributes.Equals(FileAttributes.Hidden | FileAttributes.Directory)
                                    || folder.Attributes.Equals(FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory | FileAttributes.NotContentIndexed)
                                   )
                        {
                            continue;
                        }

                        if (folder.Name.ToLower().Contains(searchPattern))
                        {
                            foundFolders.Add(folder.FullName);
                        }
                        SearchFileItems(new PathAndSearchPattern(folder.FullName, searchPattern));
                    }

                    FileInfo[] nestedFiles = currentDirectory.GetFiles();
                    foreach (var file in nestedFiles)
                    {
                        if (file.Name.ToLower().Contains(searchPattern))
                        {
                            foundFiles.Add(file.FullName);
                        }
                    }
                }
                catch (Exception)
                {
                    return;                                                                              // skip folder in recursion search
                }
            }
        }

        private async Task SearchFileItemsAsync(string path, string searchPattern)
        {
            await Task.Factory.StartNew(SearchFileItems, new PathAndSearchPattern(path, searchPattern));
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;

            if (tb.Text == "search...")
            {
                tb.Text = "";
                tb.FontStyle = FontStyles.Normal;
                tb.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;

            if (tb.Text == "")
            {
                tb.Text = "search...";
                tb.FontStyle = FontStyles.Italic;
                tb.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
    }
}