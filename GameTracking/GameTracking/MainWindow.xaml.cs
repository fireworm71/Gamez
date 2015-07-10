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

using Google.GData.Client;
using Google.GData.Spreadsheets;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using GongSolutions.Wpf.DragDrop;

namespace GameTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDropTarget
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void DoAuth()
        {
            ////////////////////////////////////////////////////////////////////////////
            // STEP 1: Configure how to perform OAuth 2.0
            ////////////////////////////////////////////////////////////////////////////

            // TODO: Update the following information with that obtained from
            // https://code.google.com/apis/console. After registering
            // your application, these will be provided for you.

            string CLIENT_ID = "906124997812-90jq6glur1kfjq5k0vdqj81t7ad214t0.apps.googleusercontent.com";

            // This is the OAuth 2.0 Client Secret retrieved
            // above.  Be sure to store this value securely.  Leaking this
            // value would enable others to act on behalf of your application!
            string CLIENT_SECRET = "";

            using (var sr = new StreamReader("../../../secret.txt"))
            {
                CLIENT_SECRET = sr.ReadLine();
            }

            // Space separated list of scopes for which to request access.
            string SCOPE = "https://spreadsheets.google.com/feeds https://docs.google.com/feeds";

            // This is the Redirect URI for installed applications.
            // If you are building a web application, you have to set your
            // Redirect URI at https://code.google.com/apis/console.
            string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

            ////////////////////////////////////////////////////////////////////////////
            // STEP 2: Set up the OAuth 2.0 object
            ////////////////////////////////////////////////////////////////////////////

            // OAuth2Parameters holds all the parameters related to OAuth 2.0.
            OAuth2Parameters parameters = new OAuth2Parameters();

            // Set your OAuth 2.0 Client Id (which you can register at
            // https://code.google.com/apis/console).
            parameters.ClientId = CLIENT_ID;

            // Set your OAuth 2.0 Client Secret, which can be obtained at
            // https://code.google.com/apis/console.
            parameters.ClientSecret = CLIENT_SECRET;

            // Set your Redirect URI, which can be registered at
            // https://code.google.com/apis/console.
            parameters.RedirectUri = REDIRECT_URI;

            ////////////////////////////////////////////////////////////////////////////
            // STEP 3: Get the Authorization URL
            ////////////////////////////////////////////////////////////////////////////

            // Set the scope for this particular service.
            parameters.Scope = SCOPE;
            
            bool needAuth = false;
            if (needAuth)
            {
                // Get the authorization url.  The user of your application must visit
                // this url in order to authorize with Google.  If you are building a
                // browser-based application, you can redirect the user to the authorization
                // url.
                string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
                Console.WriteLine(authorizationUrl);
                Console.WriteLine("Please visit the URL above to authorize your OAuth "
                  + "request token.  Once that is complete, type in your access code to "
                  + "continue...");
                parameters.AccessCode = "";// Console.ReadLine();

                ////////////////////////////////////////////////////////////////////////////
                // STEP 4: Get the Access Token
                ////////////////////////////////////////////////////////////////////////////

                // Once the user authorizes with Google, the request token can be exchanged
                // for a long-lived access token.  If you are building a browser-based
                // application, you should parse the incoming request token from the url and
                // set it in OAuthParameters before calling GetAccessToken().
                try
                {
                    OAuthUtil.GetAccessToken(parameters);
                }
                catch (Exception ex)
                {

                }
                string accessToken = parameters.AccessToken;
                Console.WriteLine("OAuth Access Token: " + accessToken);

                using (var sw = new StreamWriter("../../../token.txt"))
                {
                    sw.WriteLine(parameters.AccessToken);
                    sw.WriteLine(parameters.RefreshToken);
                }
            }

            using (var sr = new StreamReader("../../../token.txt"))
            {
                parameters.AccessToken = sr.ReadLine();
                parameters.RefreshToken = sr.ReadLine();
            }

            ////////////////////////////////////////////////////////////////////////////
            // STEP 5: Make an OAuth authorized request to Google
            ////////////////////////////////////////////////////////////////////////////

            // Initialize the variables needed to make the request
            GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, "My Great Games!", parameters);
            _service = new SpreadsheetsService("My Great Games!");
            _service.RequestFactory = requestFactory;
        }

        SpreadsheetsService _service = null;

        private AtomEntry _processedSheet;

        private ObservableCollection<GameToSell> _games = new ObservableCollection<GameToSell>();
        public ObservableCollection<GameToSell> Games
        {
            get { return _games; }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (_service == null)
            {
                DoAuth();
            }

            // Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
            SpreadsheetQuery query = new SpreadsheetQuery();
            query.Title = "Test Monkey";

            // Make a request to the API and get all spreadsheets.
            SpreadsheetFeed feed = null;
            try
            {
                feed = _service.Query(query);
            }
            catch (Exception ex)
            {
            }


            // Iterate through all of the spreadsheets returned
            var spreadsheet = feed.Entries.Single();

            // Make a request to the API to fetch information about all
            // worksheets in the spreadsheet.
            var wsFeed = ((SpreadsheetEntry)spreadsheet).Worksheets;

            var toProcess = ((WorksheetFeed)wsFeed).Entries.Single(x => x.Title.Text.Equals("To Process"));

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = toProcess.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = _service.Query(listQuery);

            var ebay = new EbayAccess();
            foreach (var entry in listFeed.Entries.OfType<ListEntry>())
            {
                _games.Add(new GameToSell(entry));
            }

            _processedSheet = ((WorksheetFeed)wsFeed).Entries.Single(x => x.Title.Text.Equals("Processed"));
        }
        
        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            bool live = true;

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = _processedSheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = _service.Query(listQuery);

            var ebay = new EbayAccess();

            ebay.GetListing(live, "171852187199");

            foreach (var game in _games)
            {
                string response;
                string id;
                if (game.Publish)
                {
                    bool success = ebay.NewListing(live, game.Upc, game.Price, game.PicturePaths.ToArray(), "", game.Description, "foo@gmail.com", 10001, game.Shipping, out response, out id);
                    if (success)
                    {
                        game.UploadResponse(response, id);

                        ListEntry newEntry = new ListEntry();
                        newEntry.Elements.Add(new ListEntry.Custom { LocalName = "Name", Value = game.Name });
                        newEntry.Elements.Add(new ListEntry.Custom { LocalName = "Condition", Value = game.Condition });
                        newEntry.Elements.Add(new ListEntry.Custom { LocalName = "ebay Item Id", Value = id });

                        listFeed.Entries.Add(newEntry);
                        listFeed.Publish();
                    }
                }
            }
        }

        public new void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = System.Windows.DragDropEffects.Link;
        }

        public new void Drop(IDropInfo dropInfo)
        {
            var game = dropInfo.TargetItem as GameToSell;
            if (game != null)
            {
                game.PicturePaths.Clear();
                foreach (string file in ((DataObject)dropInfo.Data).GetFileDropList())
                {
                    game.PicturePaths.Add(file);
                }
            }
        }
    }
}
