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
using System.Windows.Shapes;

using Google.GData.Client;
using Google.GData.Spreadsheets;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using GongSolutions.Wpf.DragDrop;
using System.Diagnostics;
using System.Windows.Navigation;

namespace GameTracking
{
    /// <summary>
    /// Interaction logic for ToProcess.xaml
    /// </summary>
    public partial class ToProcess : UserControl
    {
        public static bool live = true;

        public ToProcess()
        {
            InitializeComponent();

            DataContext = this;
        }

        private ObservableCollection<ListedGame> _games = new ObservableCollection<ListedGame>();
        public ObservableCollection<ListedGame> Games
        {
            get { return _games; }
        }
        
        public SheetsAccess Sheets
        {
            get { return (SheetsAccess)GetValue(SheetsProperty); }
            set { SetValue(SheetsProperty, value); }
        }
        public static DependencyProperty SheetsProperty = DependencyProperty.Register("Sheets", typeof(SheetsAccess), typeof(ToProcess), null);

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var toProcess = Sheets.GetToProcessSheet();

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = toProcess.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = Sheets.GetSpreadsheetService().Query(listQuery);

            foreach (var entry in listFeed.Entries.OfType<ListEntry>())
            {
                _games.Add(new ListedGame(entry));
            }
            
            // Define the URL to request the list feed of the worksheet.
            listFeedLink = Sheets.GetProcessedSheet().Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            listQuery = new ListQuery(listFeedLink.HRef.ToString());
            listFeed = Sheets.GetSpreadsheetService().Query(listQuery);

            var ebay = new EbayAccess();

            foreach (var entry in listFeed.Entries.OfType<ListEntry>())
            {
                if (string.IsNullOrEmpty(entry.Elements[3].Value))
                {
                    EbayAccess.ListingInfo info;
                    ebay.GetListingInfo(live, entry.Elements[2].Value, out info);
                    if (info.ViewUrl != null)
                    {
                        entry.Elements[3].Value = info.ViewUrl;
                    }
                    else
                    {
                        entry.Elements[3].Value = "Error getting URL!";
                    }
                    entry.Update();
                }
            }
        }
        
        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            foreach (var game in _games)
            {
                if (game.Publish)
                {
                    PublishGame(game);
                }
            }
        }

        private void PublishGame(ListedGame game)
        {
            var waitFor = Task.Run(() =>
            {
                AtomLink listFeedLink = Sheets.GetProcessedSheet().Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
                ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
                ListFeed listFeed = Sheets.GetSpreadsheetService().Query(listQuery);

                ListEntry newEntry = game.PublishToEbay(live).Result;

                if (newEntry != null)
                {
                    try
                    {
                        Sheets.GetSpreadsheetService().Insert(listFeed, newEntry);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }

        public new void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = System.Windows.DragDropEffects.Link;
        }

        public new void Drop(IDropInfo dropInfo)
        {
            var game = dropInfo.TargetItem as ListedGame;
            if (game != null)
            {
                game.PicturePaths.Clear();
                foreach (string file in ((DataObject)dropInfo.Data).GetFileDropList())
                {
                    game.PicturePaths.Add(file);
                }
            }
        }

        private void PublishSingle_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var game = (ListedGame)button.DataContext;
            PublishGame(game);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
