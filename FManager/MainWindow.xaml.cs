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
using System.Security.AccessControl;
using System.Security.Principal;

namespace FManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Remove commented code
        // TODO: Commands
        // TODO: Directory.GetFiles() - searching files
        // TODO: Context Menu
        // TODO: replace folders
        // TODO: Threading

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

            string fullNameOfFileSystemItem = System.IO.Path.Combine(currentFolderOfActiveList, (tempGrid.Children[1] as Label).Content.ToString());

            FileInfo file = new FileInfo(fullNameOfFileSystemItem);
            if (Directory.Exists(fullNameOfFileSystemItem))
            {
                currentFolderOfActiveList = System.IO.Path.Combine(currentFolderOfActiveList, (tempGrid.Children[1] as Label).Content.ToString());

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
                    list.Items.Clear();

                    string currentFolderOfActiveList = list.Tag.ToString();

                    DirectoryInfo directoryInfo = new DirectoryInfo(currentFolderOfActiveList);
                    DirectoryInfo[] directories = directoryInfo.GetDirectories();

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
                    image.Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
                }
            }

            image.Width = 30;
            image.Height = 30;

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

            if (currentFolderOfActiveList.Length > 3)                          // deleting last part of path ant goto up folder
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

            if (sender is ListBox)
            {
                destinationList = sender as ListBox;
                destinationPath = destinationList.Tag.ToString();
            }
            else if (sender is ListBoxItem)
            {
                ListBoxItem destItem = sender as ListBoxItem;
                destinationPath = System.IO.Path.Combine((destItem.Parent as ListBox).Tag.ToString(), ((destItem.Content as Grid).Children[1] as Label).Content.ToString());
                destinationList = (sender as ListBoxItem).Parent as ListBox;
            }

            if (sourceList.Tag.ToString().Length < 3 || destinationPath.Length < 3)     // if moved item is drive (or move in list of drives) - disable drop 
            {
                return;
            }

            foreach (var item in movedItems)
            {
                string fullNameOfMovedFileSystemItem = System.IO.Path.Combine(sourceList.Tag.ToString(), ((item.Content as Grid).Children[1] as Label).Content.ToString());
                string copyOfMovedFileSystemItem = System.IO.Path.Combine(destinationPath, ((item.Content as Grid).Children[1] as Label).Content.ToString());

                if (Directory.Exists(fullNameOfMovedFileSystemItem))          // if folder
                {
                    if (System.IO.Path.GetPathRoot(fullNameOfMovedFileSystemItem) != System.IO.Path.GetPathRoot(copyOfMovedFileSystemItem))   // if drives is different
                    {
                        //CopyFolders(fullNameOfMovedFileSystemItem, copyOfMovedFileSystemItem);
                        //sourceList.Items.Remove(item);
                        //if (sender is ListBox)
                        //{
                        //    destinationList.Items.Add(item);
                        //}
                        //GoToCurrentFolder(sourceList);
                        //e.Handled = true;
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
                        //try
                        //{
                        directory.MoveTo(copyOfMovedFileSystemItem);
                        sourceList.Items.Remove(item);
                        if (sender is ListBox)
                        {
                            destinationList.Items.Add(item);
                        }
                        e.Handled = true;
                        //}
                        //catch (Exception ex)
                        //{
                        //    MessageBox.Show(ex.Message);
                        //    CopyFolders(fullNameOfMovedFileSystemItem, copyOfMovedFileSystemItem);
                        //    e.Handled = true;
                        //    return;
                        //}
                    }

                }
                else if (File.Exists(fullNameOfMovedFileSystemItem))               // if file
                {
                    FileInfo movedFile = new FileInfo(fullNameOfMovedFileSystemItem);

                    if (System.IO.Path.GetPathRoot(fullNameOfMovedFileSystemItem) == System.IO.Path.GetPathRoot(copyOfMovedFileSystemItem))   // if drives is different
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

        private void CopyFolders(string sourceFolder, string targetFolder)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceFolder);

            DirectoryInfo directory = Directory.CreateDirectory(targetFolder);

            foreach (var file in sourceDir.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(targetFolder, file.Name));
            }

            foreach (var dir in sourceDir.GetDirectories())
            {
                CopyFolders(dir.FullName, System.IO.Path.Combine(targetFolder, dir.Name));
            }
        }

        private void CopyFoldersA(object pairOfPath)
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
                CopyFoldersA(new PairOfPath(dir.FullName, System.IO.Path.Combine(targetFolder, dir.Name)));
            }
        }

        private async Task CopyFoldersAsync(string sourceFolder, string targetFolder)
        {
            await Task.Factory.StartNew(CopyFoldersA, new PairOfPath(sourceFolder, targetFolder));
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
            Properties.Settings.Default.WindowPosition = this.RestoreBounds;

            Properties.Settings.Default.leftListPath = leftList.Tag.ToString();
            Properties.Settings.Default.rightListPath = rightList.Tag.ToString();

            Properties.Settings.Default.Save();
        }
    }
}