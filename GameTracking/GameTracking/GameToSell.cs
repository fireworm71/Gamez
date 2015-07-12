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
    public enum Shipping
    {
        FirstClass,
        SmallFlatRate,
    }

    public enum SheetColumns
    {
        Link,
        Condition,
        PricePaid,
        ShipAs,
        Value,
        Response,
        ID,
    }

    public class GameToSell : INotifyPropertyChanged
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

        private async void UpdateListEntry()
        {
            await Task.Run(() =>
            {
                _listEntry.Elements[(int)SheetColumns.Value].Value = _price.ToString();
                _listEntry.Elements[(int)SheetColumns.ShipAs].Value = _shipping.ToString();
                try
                {
                    _listEntry = (ListEntry)_listEntry.Update();
                }
                catch (Exception ex)
                {

                }
            });
        }

        public async Task<ListEntry> PublishToEbay(bool live)
        {
            CanPublish = false;
            var ebay = new EbayAccess();
            string ebayId = await Task.Run(() =>
                {
                    string response;
                    string id; 
                    bool succ = ebay.NewListing(live, Upc, Price, PicturePaths.ToArray(), "", Description, "foo@gmail.com", 10001, Shipping, out response, out id);

                    UploadResponse(response, id);
                    return id;
                });
            CanPublish = true;
            EbayId = ebayId;
            if (ebayId != "-1")
            {
                ListEntry newEntry = new ListEntry();
                newEntry.Elements.Add(new ListEntry.Custom { LocalName = "name", Value = Name });
                newEntry.Elements.Add(new ListEntry.Custom { LocalName = "condition", Value = Condition });
                newEntry.Elements.Add(new ListEntry.Custom { LocalName = "ebayitemid", Value = ebayId });

                string viewUrl;
                ebay.GetListing(live, ebayId, out viewUrl);
                if (viewUrl != null)
                {
                    ViewUrl = viewUrl;
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "viewlink", Value = viewUrl });
                }
                else
                {
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "viewlink", Value = "Error getting URL!" });
                }
                newEntry.Elements.Add(new ListEntry.Custom { LocalName = "soldfor", Value = "" });
                newEntry.Elements.Add(new ListEntry.Custom { LocalName = "status", Value = "Listed!" });

                return newEntry;
            }
            return null;
        }

        private string _viewUrl = null;
        public string ViewUrl
        {
            get { return _viewUrl; }
            set { _viewUrl = value; OnPropertyChanged(); }
        }

        private string _ebayId = "";
        public string EbayId
        {
            get { return _ebayId; }
            set { _ebayId = value; OnPropertyChanged(); }
        }

        private bool _canPublish = true;
        public bool CanPublish
        {
            get { return _canPublish; }
            set { _canPublish = value; OnPropertyChanged(); }
        }

        private bool _publish = false;
        public bool Publish
        {
            get { return _publish; }
            set { _publish = value; OnPropertyChanged(); }
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

        private double _price;
        public double Price
        {
            get { return _price; }
            private set { _price = value; OnPropertyChanged(); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            private set { _description = value; OnPropertyChanged(); }
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

            _url = listEntry.Elements[(int)SheetColumns.Link].Value;
            _condition = listEntry.Elements[(int)SheetColumns.Condition].Value;
            if (!Enum.TryParse<Shipping>(listEntry.Elements[(int)SheetColumns.ShipAs].Value, out _shipping))
            {
                Shipping = Shipping.FirstClass;
            }
            _details = new GameDetailer(_url, _condition);
            Initalize();
        }

        async void Initalize()
        {
            await _details.FetchPage();

            Name = _details.GetName();
            Upc = _details.GetUPC();
            Platform = _details.GetPlatform();
            Price = Math.Round(_details.GetSellingPrice(1.1f)) - 0.05;
            Description = _details.GetDescription();

            _listEntry.Elements[(int)SheetColumns.Value].Value = Price.ToString();

            UpdateListEntry();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public async void UploadResponse(string response, string id)
        {
            await Task.Run(() =>
            {
                _listEntry.Elements[(int)SheetColumns.Response].Value = response;
                _listEntry.Elements[(int)SheetColumns.ID].Value = id;
                try
                {
                    _listEntry = (ListEntry)_listEntry.Update();
                }
                catch (Exception ex)
                {

                }
            });
        }
    }
}
