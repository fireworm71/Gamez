using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameTracking
{
    public class GameDetailer
    {
        string _url;
        string _page;
        string _condition;
        public GameDetailer(string url, string condition)
        {
            _url = url;
            _condition = condition;
        }

        public async Task FetchPage()
        {
            if (string.IsNullOrEmpty(_url))
            {
                _page = "";
                return;
            }
            var priceClient = new WebClient();
            priceClient.BaseAddress = _url;
            _page = await priceClient.DownloadStringTaskAsync("");
        }

        private double GetPrice(string priceType)
        {
            int startIndex = _page.IndexOf(string.Format("id=\"{0}\"", priceType));
            string toRefine = _page.Substring(startIndex, 200);
            int dollarIndex = toRefine.IndexOf('$');
            if (dollarIndex < 0)
            {
                return 0.0;
            }
            int dotIndex = toRefine.IndexOf('.', dollarIndex);
            string priceString = toRefine.Substring(dollarIndex + 1, (dotIndex - (dollarIndex)) + 2);
            return double.Parse(priceString);
        }

        public int GetReleaseYear()
        {
            int startIndex = _page.IndexOf("\"date\">") + "\"date\">".Length;
            string toRefine = _page.Substring(startIndex, 200);
            int stopIndex = toRefine.IndexOf('<');
            string priceString = toRefine.Substring(0, stopIndex);
            try
            {
                var date = DateTime.Parse(priceString);
                return date.Year;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public double GetSellingPrice(double markup)
        {
            double used = GetPrice("used_price");
            double complete = 0;
            double new_ = 0;
            if (_condition == "CiB" || _condition == "WithInstructions")
            {
                complete = GetPrice("complete_price");
            }
            if (_condition == "NEW")
            {
                new_ = GetPrice("new_price");
                complete = GetPrice("complete_price");
            }

            double max = used;
            if (_condition == "WithInstructions")
            {
                if (complete > max)
                {
                    max += complete;
                    max /= 2.0;
                }
                else
                {
                    max *= 1.2;
                }
            }
            else
            {
                if (complete > max)
                {
                    max = complete;
                }
                if (new_ > max)
                {
                    max = new_;
                }
            }


            return max * markup;
        }

        public string GetName()
        {
			int name_start = _page.IndexOf("<title>") + "<title>".Length;
			string name_block = _page.Substring(name_start, 100);
			int name_end = name_block.IndexOf("Prices");
            return name_block.Substring(0, name_end).Trim();
        }

        public string GetPlatform()
        {
            int name_start = _page.IndexOf("<title>") + "<title>".Length;
            name_start = _page.IndexOf("(", name_start);
			string name_block = _page.Substring(name_start, 100);
			int name_end = name_block.IndexOf(") |");
            if (name_end < 0)
            {
                return "???";
            }
            return name_block.Substring(1, name_end - 1).Trim();
        }

        public string GetUPC()
        {
            int startIndex = _page.IndexOf("UPC:");
            if (startIndex < 0)
            {
                return "???";
            }
            string toRefine = _page.Substring(startIndex, 200);
            int dollarIndex = toRefine.IndexOf('>');
            int dotIndex = toRefine.IndexOf('<', dollarIndex);
            string upcString = toRefine.Substring(dollarIndex + 1, (dotIndex - (dollarIndex)) - 1);
            upcString = upcString.Replace('\\', ' ').Replace('n', ' ').Trim();
            return upcString;
        }

        public string GetCompletionText()
        {
            string completeText = "";
            switch (_condition)
            { 
                case "CiB":
                    completeText = "game, instructions, and original box";
                    break;
                case "New":
                    completeText = "factory sealed";
                    break;
                case "WithInstructions":
                    completeText = "game and instructions";
                    break;
                case "WithCase":
                    completeText = "game and original box";
                    break;
                default:
                    completeText = "game only";
                    break;
            }
            return completeText;
        }

        public string GetDescription(bool hasCase, bool hasInstructions)
        {
            string completeText = GetCompletionText();
            string title = GetName();
            string platform = GetPlatform();

            string desc = "As pictured, {0} for {1}, {2}, tested, working and ready for your enjoyment!<br><br>This game is guaranteed to work, or your money back!";

            return string.Format(desc, title, platform, completeText);
        }
    }
}
