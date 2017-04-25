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
        // TODO: сохранение габаритов и расположения, текущей папки;
        // TODO: копирование, перемещение(drag&Drop) и тд commands
        // TODO: Directory.GetFiles() - searching files
        // TODO: Directories copiing from drive to another drive
        // TODO: Context Menu

        public MainWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, new RoutedEventHandler(EventBasedMouseLeftButtonHandler));   // for ListBoxItem, because native event MouseDown don't rise 
            InRootOfFileSystem(leftList);
            InRootOfFileSystem(rightList);
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
                GoTo(parrentOfSender);
            }
            else if (File.Exists(fullNameOfFileSystemItem))                                                              // open file
            {
                System.Diagnostics.Process.Start(fullNameOfFileSystemItem);
            }
        }

        private void GoTo(ListBox list)
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
            listBoxItem.AllowDrop = true;
            listBoxItem.Drop += List_Drop;

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
                GoTo(tempList);
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
                    GoTo(listBox);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    listBox.Tag = previosPath;
                    GoTo(listBox);
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
            }

            if (sourceList.Tag.ToString().Length < 3 || destinationPath.Length < 3)     // if moved item is drive (or move in list of drives) - disable drop 
            {
                return;
            }

            foreach (var item in movedItems)
            {
                string fullNameOfMovedFileSystemItem = System.IO.Path.Combine(sourceList.Tag.ToString(), ((item.Content as Grid).Children[1] as Label).Content.ToString());
                string copyOfMovedFileSystemItem = System.IO.Path.Combine(destinationPath, ((item.Content as Grid).Children[1] as Label).Content.ToString());

                if (Directory.Exists(fullNameOfMovedFileSystemItem))
                {
                    DirectoryInfo directory = new DirectoryInfo(fullNameOfMovedFileSystemItem);
                    try
                    {
                        directory.MoveTo(copyOfMovedFileSystemItem);
                        sourceList.Items.Remove(item);
                        if (sender is ListBox)
                        {
                            destinationList.Items.Add(item);
                        }
                        e.Handled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }
                else if (File.Exists(fullNameOfMovedFileSystemItem))
                {
                    FileInfo movedFile = new FileInfo(fullNameOfMovedFileSystemItem);
                    try
                    {
                        movedFile.MoveTo(copyOfMovedFileSystemItem);
                        sourceList.Items.Remove(item);
                        if (sender is ListBox)
                        {
                            destinationList.Items.Add(item);
                        }
                        e.Handled = true;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        MessageBox.Show(ex.Message + " Try run programm as administrator");
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
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
    }
}