using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SkaterCapture.Annotations;
using SkaterCapture.Models;

namespace SkaterCapture
{
    /// <summary>
    /// Interaction logic for PickShotWindow.xaml
    /// </summary>
    public partial class PickShotWindow : Window, INotifyPropertyChanged
    {
        private string _currentFile;

        public PickShotWindow(ImageCollection collection)
        {
            InitializeComponent();
            DataContext = this;
            ShotsList.ItemsSource = collection.Files;
        }

        public string CurrentFile
        {
            get { return _currentFile; }
            set
            {
                if (value == _currentFile) return;
                _currentFile = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShotsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentFile = (string) ShotsList.SelectedItem;
        }
    }
}
