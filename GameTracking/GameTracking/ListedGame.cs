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
    public enum ProcessedSheetColumns
    {
        Name,
        Condition,
        EbayID,
        Link,
        SoldFor,
        Status,
        GameID,
    }

    public class ListedGame : INotifyPropertyChanged
    {
        private async void UpdateListEntry()
        {
            await Task.Run(() =>
            {
                try
                {
                    _listEntry = (ListEntry)_listEntry.Update();
                }
                catch (Exception ex)
                {

                }
            });
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

        private ListEntry _listEntry;
        public ListEntry ListEntry
        {
            get { return _listEntry; }
        }

        private EbayAccess.ListingInfo _listingInfo;
        public EbayAccess.ListingInfo ListingInfo
        {
            get { return _listingInfo; }
            set { _listingInfo = value; OnPropertyChanged(); }
        }

        public ListedGame(ListEntry listEntry)
        {
            _listEntry = listEntry;

            _condition = listEntry.Elements[(int)ProcessedSheetColumns.Condition].Value;
            Initalize();
        }

        async void Initalize()
        {
            Name = _listEntry.Elements[(int)ProcessedSheetColumns.Name].Value;
            string ebayId = _listEntry.Elements[(int)ProcessedSheetColumns.EbayID].Value;

            var ebay = new EbayAccess();
            EbayAccess.ListingInfo info;
            await Task.Run(() =>
            {
                ebay.GetListingInfo(ToProcess.live, ebayId, out info);
                ViewUrl = info.ViewUrl;
                ListingInfo = info;

                _listEntry.Elements[(int)ProcessedSheetColumns.Status].Value = info.CurrentStatus.ToString();
                if (info.CurrentStatus == EbayAccess.EbayStatus.Sold)
                {
                    _listEntry.Elements[(int)ProcessedSheetColumns.SoldFor].Value = info.SoldPrice.ToString();
                }
            });


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
