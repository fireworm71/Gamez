using Google.GData.Client;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTracking
{
    public class SheetsAccess
    {
        SpreadsheetsService _service = null;

        AtomEntry _toProcessSheet = null;
        AtomEntry _processedSheet = null;
        AtomEntry _everythingSheet = null;

        public AtomEntry GetToProcessSheet()
        {
            GetSpreadsheetService();
            return _toProcessSheet;
        }
        public AtomEntry GetProcessedSheet()
        {
            GetSpreadsheetService();
            return _processedSheet;
        }
        public AtomEntry GetEverythingSheet()
        {
            GetSpreadsheetService();
            return _everythingSheet;
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

        public SpreadsheetsService GetSpreadsheetService()
        {
            if (_service == null)
            {
                DoAuth();

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

                _toProcessSheet = ((WorksheetFeed)wsFeed).Entries.Single(x => x.Title.Text.Equals("To Process"));
                _processedSheet = ((WorksheetFeed)wsFeed).Entries.Single(x => x.Title.Text.Equals("Processed"));
                _everythingSheet = ((WorksheetFeed)wsFeed).Entries.Single(x => x.Title.Text.Equals("Everything"));
            }

            return _service;
        }
    }
}