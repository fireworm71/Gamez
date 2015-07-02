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

namespace GameTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
            
            // Get the authorization url.  The user of your application must visit
            // this url in order to authorize with Google.  If you are building a
            // browser-based application, you can redirect the user to the authorization
            // url.
            //string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
            //Console.WriteLine(authorizationUrl);
            Console.WriteLine("Please visit the URL above to authorize your OAuth "
              + "request token.  Once that is complete, type in your access code to "
              + "continue...");
           // parameters.AccessCode = "";// Console.ReadLine();

            ////////////////////////////////////////////////////////////////////////////
            // STEP 4: Get the Access Token
            ////////////////////////////////////////////////////////////////////////////

            // Once the user authorizes with Google, the request token can be exchanged
            // for a long-lived access token.  If you are building a browser-based
            // application, you should parse the incoming request token from the url and
            // set it in OAuthParameters before calling GetAccessToken().
            try
            {
                //OAuthUtil.GetAccessToken(parameters);
            }
            catch (Exception ex)
            {

            }
            string accessToken = parameters.AccessToken;
            Console.WriteLine("OAuth Access Token: " + accessToken);

            string accessToken2 = "";
            parameters.AccessToken = accessToken2;

            ////////////////////////////////////////////////////////////////////////////
            // STEP 5: Make an OAuth authorized request to Google
            ////////////////////////////////////////////////////////////////////////////

            // Initialize the variables needed to make the request
            GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, "My Great Games!", parameters);
            service = new SpreadsheetsService("My Great Games!");
            service.RequestFactory = requestFactory;
        }

        SpreadsheetsService service;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoAuth();

            // Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
            SpreadsheetQuery query = new SpreadsheetQuery();
            query.Title = "Test Monkey";

            // Make a request to the API and get all spreadsheets.
            SpreadsheetFeed feed = null;
            try
            {
                 feed = service.Query(query);
            }
            catch (Exception ex)
            {
            }


            // Iterate through all of the spreadsheets returned
            foreach (SpreadsheetEntry spreadsheet in feed.Entries)
            {
                // Print the title of this spreadsheet to the screen
                Console.WriteLine(spreadsheet.Title.Text);


                // Make a request to the API to fetch information about all
                // worksheets in the spreadsheet.
                WorksheetFeed wsFeed = spreadsheet.Worksheets;

                // Iterate through each worksheet in the spreadsheet.
                foreach (WorksheetEntry entry in wsFeed.Entries)
                {
                    // Define the URL to request the list feed of the worksheet.
                    AtomLink listFeedLink = entry.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

                    // Fetch the list feed of the worksheet.
                    ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
                    ListFeed listFeed = service.Query(listQuery);

                    listFeed.Entries.RemoveAt(1);
                    ((ListEntry)listFeed.Entries[0]).Elements[2].Value = "100.0";
                    ((ListEntry)listFeed.Entries[0]).Update();

                    listFeed.Publish();
                }
            }
        }

        /*
         * https://developers.google.com/google-apps/spreadsheets/
      // Define the URL to request the list feed of the worksheet.
      AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

      // Fetch the list feed of the worksheet.
      ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
      ListFeed listFeed = service.Query(listQuery);

      // TODO: Choose a row more intelligently based on your app's needs.
      ListEntry row = (ListEntry)listFeed.Entries[0];

      // Update the row's data.
      foreach (ListEntry.Custom element in row.Elements)
      {
        if (element.LocalName == "firstname")
        {
          element.Value = "Sarah";
        }
        if (element.LocalName == "lastname")
        {
          element.Value = "Hunt";
        }
        if (element.LocalName == "age")
        {
          element.Value = "32";
        }
        if (element.LocalName == "height")
        {
          element.Value = "154";
        }
      }

      // Save the row using the API.
      row.Update();
    }
         */

         /*
         * 
      // Define the URL to request the list feed of the worksheet.
      AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

      // Fetch the list feed of the worksheet.
      ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
      ListFeed listFeed = service.Query(listQuery);

      // Create a local representation of the new row.
      ListEntry row = new ListEntry();
      row.Elements.Add(new ListEntry.Custom() { LocalName = "firstname", Value = "Joe" });
      row.Elements.Add(new ListEntry.Custom() { LocalName = "lastname", Value = "Smith" });
      row.Elements.Add(new ListEntry.Custom() { LocalName = "age", Value = "26" });
      row.Elements.Add(new ListEntry.Custom() { LocalName = "height", Value = "176" });

      // Send the new row to the API for insertion.
      service.Insert(listFeed, row);
         * /
          * */
    }
}
