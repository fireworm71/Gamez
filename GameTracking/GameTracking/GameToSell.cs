using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GameTracking
{
    public enum Shipping
    {
        FirstClass,
        SmallFlatRate,
        MediumFlatRate,
        LargeFlatRate,
        PriorityByWeight,
    }

    public enum ToProcessSheetColumns
    {
        Link,
        Condition,
        PricePaid,
        ShipAs,
        Value,
        Response,
        ID,
        BundleID,
    }

    public class ToSell: INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        private double _price;
        public double Price
        {
            get { return _price; }
            set { _price = value; OnPropertyChanged(); }
        }

        private int _bundleId;
        public int BundleId
        {
            get { return _bundleId; }
            set { _bundleId = value; OnPropertyChanged(); }
        }

        private Shipping _shipping = Shipping.FirstClass;
        public Shipping Shipping
        {
            get { return _shipping; }
            set
            {
                _shipping = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Shipping> ShippingMethods
        {
            get
            {
                return Enum.GetValues(typeof(Shipping)).Cast<Shipping>();
            }
        }

        private int _shippingLbs = 0;
        public int ShippingLbs
        {
            get { return _shippingLbs; }
            set { _shippingLbs = value; OnPropertyChanged(); UpdateShipping(); }
        }

        private int _shippingOz = 0;
        public int ShippingOz
        {
            get { return _shippingOz; }
            set { _shippingOz = value; OnPropertyChanged(); UpdateShipping(); }
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

        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        private string _upc;
        public string Upc
        {
            get { return _upc; }
            set { _upc = value; OnPropertyChanged(); }
        }

        private EbayAccess.ListingInfo _listingInfo;
        public EbayAccess.ListingInfo ListingInfo
        {
            get { return _listingInfo; }
            set { _listingInfo = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _picturePaths = new ObservableCollection<string>();
        public ObservableCollection<string> PicturePaths
        {
            get { return _picturePaths; }
        }

        private void UpdateShipping()
        {
            if (ShippingLbs > 0 || ShippingOz > 13)
            {
                if (Shipping == GameTracking.Shipping.FirstClass)
                {
                    Shipping = GameTracking.Shipping.PriorityByWeight;
                }
            }
            else
            {
                if (Shipping == GameTracking.Shipping.PriorityByWeight)
                {
                    Shipping = GameTracking.Shipping.FirstClass;
                }
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

    public class GameToSell : ToSell
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

        private string _platform;
        public string Platform
        {
            get { return _platform; }
            private set { _platform = value; OnPropertyChanged(); }
        }

        private bool _hasInstructions;
        public bool HasInstructions
        {
            get { return _hasInstructions; }
            set
            {
                _hasInstructions = value;
                OnPropertyChanged();
                Description = _details.GetDescription(HasCase, HasInstructions);
            }
        }

        private bool _hasCase;
        public bool HasCase
        {
            get { return _hasCase; }
            set
            {
                _hasCase = value;
                OnPropertyChanged();
                Description = _details.GetDescription(HasCase, HasInstructions);
            }
        }

        private ListEntry _listEntry;
        public ListEntry ListEntry
        {
            get { return _listEntry; }
        }

        private GameDetailer _details;
        public GameDetailer Details
        {
            get { return _details; }
        }

        public GameToSell(ListEntry listEntry)
        {
            _listEntry = listEntry;

            _url = listEntry.Elements[(int)ToProcessSheetColumns.Link].Value;
            _condition = listEntry.Elements[(int)ToProcessSheetColumns.Condition].Value;
            Shipping shipping;
            if (!Enum.TryParse<Shipping>(listEntry.Elements[(int)ToProcessSheetColumns.ShipAs].Value, out shipping))
            {
                Shipping = Shipping.FirstClass;
            }
            else
            {
                Shipping = shipping;
            }
            _details = new GameDetailer(_url, _condition);
            Initalize();
        }

        async void Initalize()
        {
            await _details.FetchPage();
            _details.GetReleaseYear();

            Name = _details.GetName();
            Upc = _details.GetUPC();
            Platform = _details.GetPlatform();
            Price = Math.Round(_details.GetSellingPrice(1.1f)) - 0.05;
            Description = _details.GetDescription(HasCase, HasInstructions);

            _listEntry.Elements[(int)ToProcessSheetColumns.Value].Value = Price.ToString();

            string ebayId = _listEntry.Elements[(int)ToProcessSheetColumns.ID].Value;

            if (Initalized != null)
            {
                Initalized(this, new EventArgs());
            }

            var ebay = new EbayAccess();
            EbayAccess.ListingInfo info;
            await Task.Run(() =>
            {
                ebay.GetListingInfo(ToProcess.live, ebayId, out info);
                ViewUrl = info.ViewUrl;
                ListingInfo = info;
            });


            UpdateListEntry();
        }

        public event EventHandler Initalized;

        private async void UpdateListEntry()
        {
            await Task.Run(() =>
            {
                _listEntry.Elements[(int)ToProcessSheetColumns.Value].Value = Price.ToString();
                _listEntry.Elements[(int)ToProcessSheetColumns.ShipAs].Value = Shipping.ToString();
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
                string titleOverride = string.Format("{0} ({1}", Name, Platform);
                int releaseYear = _details.GetReleaseYear();
                if (_details.GetReleaseYear() > 1)
                {
                    titleOverride += string.Format(", {0})", releaseYear);
                }
                else
                {
                    titleOverride += ")";
                }
                bool succ = ebay.NewListing(live, Upc, Price, PicturePaths.ToArray(), titleOverride, Description, Shipping, EbayAccess.EbayCategory.Game, out response, out id);

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

                EbayAccess.ListingInfo info;
                ebay.GetListingInfo(live, ebayId, out info);
                if (info.ViewUrl != null)
                {
                    ViewUrl = info.ViewUrl;
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "viewlink", Value = info.ViewUrl });
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

        public async void UploadResponse(string response, string id)
        {
            await Task.Run(() =>
            {
                _listEntry.Elements[(int)ToProcessSheetColumns.Response].Value = response;
                _listEntry.Elements[(int)ToProcessSheetColumns.ID].Value = id;
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

    public class BundleToSell : ToSell
    {
        public BundleToSell()
        {
            Games.CollectionChanged += Games_CollectionChanged;
        }

        void Games_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach(var game in Games)
            {
                game.Initalized += game_Initalized;
            }

            ViewUrl = Games[0].ViewUrl;
            ListingInfo = Games[0].ListingInfo;
        }

        void game_Initalized(object sender, EventArgs e)
        {
            double newPrice = 0.0;
            string newName = "";
            string newDescription = "As pictured: ";
            string newPricebreakdown = "";

            int count = 0;
            foreach(var game in Games)
            {
                newPrice += game.Price;
                newName += game.Name + "\n";
                newPricebreakdown += game.Price.ToString() + "\n";

                if (count != 0)
                {
                    newDescription += string.Format("\n\t{0}: {1} ({2})", (char)('A' + count), game.Name, game.Details.GetCompletionText());
                }
                else
                {
                    newDescription += string.Format("\n\t{0}: {1}", (char)('A' + count), game.Name);
                    if (Extras != null)
                    {
                        var extras = Extras.Split(',');
                        foreach (var extra in extras)
                        {
                            count++;
                            newDescription += string.Format("\n\t{0}: {1}", (char)('A' + count), extra);
                        }
                    }
                }
                count++;
            }

            newDescription += "\n\nEverything is tested and guaranteed to work, or your money back!";

            Price = newPrice;
            Name = newName;
            Description = newDescription;
            PriceBreakdown = newPrice.ToString() + "\n" + newPricebreakdown;
        }

        private string _priceBreakdown = "";
        public string PriceBreakdown
        {
            get { return _priceBreakdown; }
            set { _priceBreakdown = value; OnPropertyChanged(); }
        }

        private ObservableCollection<GameToSell> _games = new ObservableCollection<GameToSell>();
        public ObservableCollection<GameToSell> Games
        {
            get { return _games; }
        }

        private string _title = "???";
        public string Title
        {
            get { return _title; }
            set { _title = value.Substring(0, Math.Min(value.Length, 80)); OnPropertyChanged(); }
        }

        private string _extras;
        public string Extras
        {
            get { return _extras; }
            set { _extras = value; OnPropertyChanged(); game_Initalized(this, new EventArgs()); }
        }

        public async Task<List<ListEntry>> PublishToEbay(bool live)
        {
            CanPublish = false;
            var ebay = new EbayAccess();
            string ebayId = await Task.Run(() =>
            {
                string response;
                string id;

                bool succ = ebay.NewListing(live, Games[0].Upc, Price, PicturePaths.ToArray(), Title, 
                    Description.Replace("\n", "<br>").Replace("\t", "<span class=\"Apple-tab-span\" style=\"white-space:pre\"></span>"), 
                    Shipping, EbayAccess.EbayCategory.Console, out response, out id, true, ShippingLbs, ShippingOz);

                UploadResponse(response, id);
                return id;
            });
            CanPublish = true;
            EbayId = ebayId;
            if (ebayId != "-1")
            {
                var entriesToReturn = new List<ListEntry>();
                foreach (var game in Games)
                {
                    ListEntry newEntry = new ListEntry();
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "name", Value = game.Name });
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "condition", Value = game.Condition });
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "ebayitemid", Value = ebayId });

                    EbayAccess.ListingInfo info;
                    ebay.GetListingInfo(live, ebayId, out info);
                    if (info.ViewUrl != null)
                    {
                        ViewUrl = info.ViewUrl;
                        newEntry.Elements.Add(new ListEntry.Custom { LocalName = "viewlink", Value = info.ViewUrl });
                    }
                    else
                    {
                        newEntry.Elements.Add(new ListEntry.Custom { LocalName = "viewlink", Value = "Error getting URL!" });
                    }
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "soldfor", Value = "" });
                    newEntry.Elements.Add(new ListEntry.Custom { LocalName = "status", Value = "Listed!" });

                    entriesToReturn.Add(newEntry);
                }

                return entriesToReturn;
            }
            return null;
        }

        public async void UploadResponse(string response, string id)
        {
            await Task.Run(() =>
            {
                foreach (var game in Games)
                {
                    game.UploadResponse(response, id);
                }
            });
        }
    }
}
