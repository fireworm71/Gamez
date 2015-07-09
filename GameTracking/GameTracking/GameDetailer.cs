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
            var priceClient = new WebClient();
            priceClient.BaseAddress = _url;
            _page = await priceClient.DownloadStringTaskAsync("");
        }

        private double GetPrice(string priceType)
        {
            int startIndex = _page.IndexOf(string.Format("id=\"{0}\"", priceType));
            string toRefine = _page.Substring(startIndex, 200);
            int dollarIndex = toRefine.IndexOf('$');
            int dotIndex = toRefine.IndexOf('.', dollarIndex);
            string priceString = toRefine.Substring(dollarIndex + 1, (dotIndex - (dollarIndex)) + 2);
            return double.Parse(priceString);
        }

        public double GetSellingPrice(double markup)
        {
            double used = GetPrice("used_price");
            double complete = 0;
            double new_ = 0;
            if (_condition == "CiB")
            {
                complete = GetPrice("complete_price");
            }
            if (_condition == "NEW")
            {
                new_ = GetPrice("new_price");
                complete = GetPrice("complete_price");
            }

            double max = used;
            if (complete > max)
            {
                max = complete;
            }
            if (new_ > max)
            {
                max = new_;
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
            return name_block.Substring(1, name_end - 1).Trim();
        }

        public string GetUPC()
        {
            int startIndex = _page.IndexOf("UPC:");
            string toRefine = _page.Substring(startIndex, 200);
            int dollarIndex = toRefine.IndexOf('>');
            int dotIndex = toRefine.IndexOf('<', dollarIndex);
            string upcString = toRefine.Substring(dollarIndex + 1, (dotIndex - (dollarIndex)) - 1);
            upcString = upcString.Replace('\\', ' ').Replace('n', ' ').Trim();
            return upcString;
        }

        public string GetDescription()
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
                default:
                    completeText = "game only";
                    break;
            }

            string title = GetName();
            string platform = GetPlatform();

            string desc = "As pictured, {0} for {1}, {2}, tested, working and ready for your enjoyment!<br><br>This game is guaranteed to work, or your money back!";

            return string.Format(desc, title, platform, completeText);
        }
    }
}
