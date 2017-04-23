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

namespace FManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //TODO:  сохранение габаритов и расположения, текущей папки;
        //TODO: копирование, перемещение(drag&Drop) и тд commands
        // Directory.GetFiles() - searching files

        public MainWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, new RoutedEventHandler(EventBasedMouseLeftButtonHandler));
            InRootOfFileSystem(leftList);
            InRootOfFileSystem(rightList);
        }

        private void InRootOfFileSystem(ListBox list)
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

            string sl = "\\";
            if (currentFolderOfActiveList == string.Empty || currentFolderOfActiveList[currentFolderOfActiveList.Length - 1].Equals('\\'))
            {
                sl = string.Empty;
            }
            string fullFileName = currentFolderOfActiveList + sl + (tempGrid.Children[1] as Label).Content;

            FileInfo file = new FileInfo(fullFileName);

            if (file.Extension == string.Empty)   /// if folder
            {
                if (currentFolderOfActiveList != string.Empty)
                {
                    string s = "\\";
                    if (currentFolderOfActiveList[currentFolderOfActiveList.Length - 1].Equals('\\'))
                    {
                        s = string.Empty;
                    }
                    currentFolderOfActiveList = currentFolderOfActiveList + s + (tempGrid.Children[1] as Label).Content.ToString();
                }
                else
                {
                    currentFolderOfActiveList = (tempGrid.Children[1] as Label).Content.ToString().Substring(0, 3);
                }
                parrentOfSender.Tag = currentFolderOfActiveList;
                GoTo(parrentOfSender);
            }
            else
            {
                System.Diagnostics.Process.Start(fullFileName);
            }
        }

        private void GoTo(ListBox list)
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

            if (currentFolderOfActiveList.Length > 3)
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

                listBox.Tag = changedPath;
                try
                {
                    GoTo(listBox);
                }
                catch (Exception)
                {
                    MessageBox.Show("Path not found");
                    listBox.Tag = previosPath;
                    GoTo(listBox);
                }
            }
        }

        private void EventBasedMouseLeftButtonHandler(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            ListBox sourceList = item.Parent as ListBox;
            sourceList.AllowDrop = false;

            if (sourceList.Name == "leftList")
            {
                rightList.AllowDrop = true;
            }
            else
            {
                leftList.AllowDrop = true;
            }
            DragDrop.DoDragDrop(item, item, DragDropEffects.Move);
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            ListBoxItem movedItem = e.Data.GetData(typeof(ListBoxItem)) as ListBoxItem;
            string pathOfMovedItem = (movedItem.Parent as ListBox).Tag.ToString();

            ListBox destinationList = sender as ListBox;

            if (pathOfMovedItem.Length > 2 && destinationList.Tag.ToString().Length > 2)     // if moved item is drive - disable drop (or move in list of drives)
            {
                ListBox sourceList = movedItem.Parent as ListBox;
                sourceList.Items.Remove(movedItem);
                destinationList.Items.Add(movedItem);
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