using GongSolutions.Wpf.DragDrop;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GameTracking
{
    public class GameToSell : INotifyPropertyChanged, IDropTarget
    {
        private string _url;
        public string Url
        {
            get { return _url; }
            private set { _url = value; OnPropertyChanged(); }
        }
        
        private string _condition;
        public string Condition
        {
            get { return _condition; }
            private set { _condition = value; OnPropertyChanged(); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            private set { _name = value; OnPropertyChanged(); }
        }

        private string _platform;
        public string Platform
        {
            get { return _platform; }
            private set { _platform = value; OnPropertyChanged(); }
        }

        private string _upc;
        public string Upc
        {
            get { return _upc; }
            private set { _upc = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _picturePaths = new ObservableCollection<string>();
        public ObservableCollection<string> PicturePaths
        {
            get { return _picturePaths; }
        }

        private ListEntry _listEntry;
        public ListEntry ListEntry
        {
            get { return _listEntry; }
        }

        private GameDetailer _details;

        public GameToSell(ListEntry listEntry)
        {
            _listEntry = listEntry;

            _url = listEntry.Elements[0].Value;
            _condition = listEntry.Elements[1].Value;
            _details = new GameDetailer(_url, _condition);
            Initalize();
        }

        async void Initalize()
        {
            await _details.FetchPage();

            Name = _details.GetName();
            Upc = _details.GetUPC();
            Platform = _details.GetPlatform();

            //double price = Math.Round(_details.GetSellingPrice(1.0f)) - 0.05;
            //string desc = _details.GetDescription();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            //throw new NotImplementedException();
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = System.Windows.DragDropEffects.Link;
        }

        public void Drop(IDropInfo dropInfo)
        {
            //throw new NotImplementedException();
        }
    }
}
