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
    enum DataBaseColumn
    {
        Name,
        Platform,
        Condition,
        Link,
        Origination,
        PurchasePrice,
        Status,
        SellingPrice,
        EstPrice,
        GameID,
    }

    public class GameDatabaseEntry : INotifyPropertyChanged
    {
        private Shipping _shipping = Shipping.FirstClass;
        public Shipping Shipping
        {
            get { return _shipping; }
            set
            {
                _shipping = value;
                OnPropertyChanged();
                UpdateListEntry();
            }
        }

        public IEnumerable<Shipping> ShippingMethods
        {
            get
            {
                return Enum.GetValues(typeof(Shipping))
                    .Cast<Shipping>();
            }
        }

        public async void UpdateListEntry()
        {
            await Task.Run(() =>
            {
                _listEntry.Elements[(int)DataBaseColumn.Name].Value = _name.ToString();
                _listEntry.Elements[(int)DataBaseColumn.EstPrice].Value = _price.ToString();
                _listEntry.Elements[(int)DataBaseColumn.GameID].Value = _gameId.ToString();
                _listEntry.Elements[(int)DataBaseColumn.Status].Value = _status;
                if (_sellingPrice > 0.0f)
                {
                    _listEntry.Elements[(int)DataBaseColumn.SellingPrice].Value = _sellingPrice.ToString();
                }
                try
                {
                    _listEntry = (ListEntry)_listEntry.Update();
                }
                catch (Exception ex)
                {

                }
            });
        }


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

        private string _status;
        public string Status
        {
            get { return _status; }
            private set { _status = value; OnPropertyChanged(); }
        }

        private double _price;
        public double Price
        {
            get { return _price; }
            private set { _price = value; OnPropertyChanged(); }
        }

        private double _sellingPrice;
        public double SellingPrice
        {
            get { return _sellingPrice; }
            private set { _sellingPrice = value; OnPropertyChanged(); }
        }

        private int _gameId;
        public int GameId
        {
            get { return _gameId; }
            private set { _gameId = value; OnPropertyChanged(); }
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

        public GameDatabaseEntry(ListEntry listEntry)
        {
            _listEntry = listEntry;

            _url = listEntry.Elements[(int)DataBaseColumn.Link].Value;
            _condition = listEntry.Elements[(int)DataBaseColumn.Condition].Value;

            _gameId = int.Parse(_listEntry.Elements[(int)DataBaseColumn.GameID].Value);
            _status = _listEntry.Elements[(int)DataBaseColumn.Status].Value;
            double sellingPrice;
            if (double.TryParse(_listEntry.Elements[(int)DataBaseColumn.SellingPrice].Value, out sellingPrice))
            {
                _sellingPrice = sellingPrice;
            }

            _details = new GameDetailer(_url, _condition);
            //if (!_status.Contains("Sold"))
            {
                Initalize();
            }
        }

        async void Initalize()
        {
            if (!string.IsNullOrEmpty(_url) && _url != "Error getting URL!")
            {
                await _details.FetchPage();

                Name = _details.GetName();
                Upc = _details.GetUPC();
                Platform = _details.GetPlatform();
                Price = _details.GetSellingPrice(1.0f);

                UpdateListEntry();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
