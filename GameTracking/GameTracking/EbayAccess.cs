using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTracking
{
    public class EbayAccess
    {
        private static ApiContext apiContext = null;

        private static string paypalEmail = null;
        private static int locationZip = 0;

        /// <summary>
        /// Populate eBay SDK ApiContext object with data from application configuration file
        /// </summary>
        /// <returns>ApiContext</returns>
        static ApiContext GetApiContext(bool live)
        {
            //apiContext is a singleton,
            //to avoid duplicate config file reading
            if (apiContext != null)
            {
                return apiContext;
            }
            else
            {
                apiContext = new ApiContext();
                ApiCredential apiCredential = new ApiCredential();

                if (live)
                {
                    //set Api Server Url
                    apiContext.SoapApiServerUrl = "https://api.ebay.com/wsapi";
                    apiContext.EPSServerUrl = "https://api.ebay.com/ws/api.dll";
                    //set Api Token to access eBay Api Server
                    using (var sw = new StreamReader("../../../ebayliveaccess.txt"))
                    {
                        apiCredential.eBayToken = sw.ReadLine();
                    }
                }
                else 
                {
                    //set Api Server Url
                    apiContext.SoapApiServerUrl = "https://api.sandbox.ebay.com/wsapi";
                    apiContext.EPSServerUrl = "https://api.sandbox.ebay.com/ws/api.dll";
                    //set Api Token to access eBay Api Server
                    using (var sw = new StreamReader("../../../ebayaccess.txt"))
                    {
                        apiCredential.eBayToken = sw.ReadLine();
                    }
                }

                apiContext.ApiCredential = apiCredential;
                //set eBay Site target to US
                apiContext.Site = SiteCodeType.US;


                using (var sw = new StreamReader("../../../ebayinfo.txt"))
                {
                    paypalEmail = sw.ReadLine();
                    locationZip = int.Parse(sw.ReadLine());
                }

                return apiContext;
            }
        }

        public enum EbayCategory
        {
            Game,
            Console,
        }

        private ItemType CreateItem(string upc, double price, string titleOverride, string description, Shipping shipping, EbayCategory ebayCategory, bool forceTitleOverride, int lbs, int oz) 
        {
            var item = new ItemType();

            item.PictureDetails = new PictureDetailsType();
            item.PictureDetails.GalleryType = GalleryTypeCodeType.Gallery;
            item.BestOfferDetails = new BestOfferDetailsType
            {
                BestOfferEnabled = true
            };

            item.IncludeRecommendations = true;

            item.Title = forceTitleOverride ? titleOverride : ""; ;
            item.Description = description;
            switch (ebayCategory)
            {
                case EbayCategory.Console:
                    item.PrimaryCategory = new CategoryType() { CategoryID = "48752" };
                    item.ConditionID = 3000;
                    break;
                case EbayCategory.Game:
                    item.PrimaryCategory = new CategoryType() { CategoryID = "139973" };
                    item.ConditionID = 4000;
                    break;
            }
            item.ProductListingDetails = new ProductListingDetailsType
            {
                UPC = upc
            };
            item.StartPrice = new AmountType() { currencyID = CurrencyCodeType.USD, Value = price };
            item.Currency = CurrencyCodeType.USD;
            item.Country = CountryCodeType.US;
            item.DispatchTimeMax = 1;
            item.ListingDuration = "Days_30";
            item.ListingType = ListingTypeCodeType.FixedPriceItem;
            item.PaymentMethods = new BuyerPaymentMethodCodeTypeCollection { BuyerPaymentMethodCodeType.PayPal };
            item.PayPalEmailAddress = paypalEmail;
            item.PostalCode = locationZip.ToString();
            item.Quantity = 1;
            item.ReturnPolicy = new ReturnPolicyType()
            {
                ReturnsAcceptedOption = "ReturnsAccepted",
                RefundOption = "MoneyBack",
                ReturnsWithinOption = "Days_14",
                ShippingCostPaidByOption = "Buyer"
            };

            string shippingMethod = "";
            decimal shippingLbs = 0;
            decimal shippingOz = 0;
            switch (shipping)
            {
                case Shipping.FirstClass:
                    shippingMethod = "USPSFirstClass";
                    if (oz == 0)
                    {
                        shippingOz = 5;
                    }
                    else
                    {
                        shippingOz = (decimal)oz;
                    }
                    break;
                case Shipping.PriorityByWeight:
                    shippingMethod = "USPSPriority";
                    if (lbs == 0)
                    {
                        shippingLbs = 2;
                    }
                    else
                    {
                        shippingLbs = (decimal)lbs;
                        shippingOz = (decimal)oz;
                    }
                    break;
                case Shipping.SmallFlatRate:
                    shippingMethod = "USPSPriorityMailSmallFlatRateBox";
                    shippingLbs = 5;
                    break;
                case Shipping.MediumFlatRate:
                    shippingMethod = "USPSPriorityMailFlatRateBox";
                    shippingLbs = 10;
                    break;
                case Shipping.LargeFlatRate:
                    shippingMethod = "USPSPriorityMailLargeFlatRateBox";
                    shippingLbs = 15;
                    break;
                default:
                    break;
            }

            item.ShippingDetails = new ShippingDetailsType
            {
                ShippingType = ShippingTypeCodeType.Calculated,
                CalculatedShippingRate = new CalculatedShippingRateType
                {
                    OriginatingPostalCode = locationZip.ToString(),
                    PackagingHandlingCosts = new AmountType { currencyID = CurrencyCodeType.USD, Value = 0.0 },
                    ShippingPackage = ShippingPackageCodeType.PackageThickEnvelope,
                    WeightMajor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "lbs", Value = shippingLbs },
                    WeightMinor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "oz", Value = shippingOz }
                },
                ShippingServiceOptions = new ShippingServiceOptionsTypeCollection{
                    new ShippingServiceOptionsType{
                        ShippingService = shippingMethod,
                        ShippingServicePriority = 1,
                    }
                }
            };

            return item;
        }

        public bool NewListing(bool live, string upc, double price, string[] picFiles, string titleOverride, string description, Shipping shipping, EbayCategory ebayCategory, out string response, out string id, bool forceTitleOverride = false, 
            int lbs = 0, int oz = 0)
        {
            ApiContext apiContext = GetApiContext(live);
            bool useTitleOverride = forceTitleOverride;

            {
                var addItem = new VerifyAddFixedPriceItemCall(apiContext)
                {
                    Item = CreateItem(upc, price, titleOverride, description, shipping, ebayCategory, useTitleOverride, lbs, oz), 
                    PictureFileList = new StringCollection(picFiles),
                };

                try
                {
                    addItem.Execute();
                }
                catch (Exception ex)
                {
                    useTitleOverride = true;
                }
            }
            {
                var addItem = new AddFixedPriceItemCall(apiContext) 
                {
                    Item = CreateItem(upc, price, titleOverride, description, shipping, ebayCategory, useTitleOverride, lbs, oz), 
                    PictureFileList = new StringCollection(picFiles),
                };

                try
                {
                    addItem.Execute();
                }
                catch (Exception ex)
                {
                    id = "-1";
                    response = ex.ToString();
                    return false;
                }
                response = "OK!";
                id = addItem.ApiResponse.ItemID;
            }

            return true;
        }

        public enum EbayStatus
        {
            Unlisted,
            InProgressAuction,
            InProgressBuyItNow,
            Sold,
            Unsold,
        }

        public class ListingInfo
        {
            public string ViewUrl { get; set; }
            public double ListPrice { get; set; }
            public EbayStatus CurrentStatus { get; set; }
            public double SoldPrice { get; set; }
        }

        public void GetListingInfo(bool live, string id, out ListingInfo info)
        {
            ApiContext apiContext = GetApiContext(live);

            var getItem = new GetItemCall(apiContext);
            getItem.ItemID = id;
            try
            {
                getItem.Execute();
            }
            catch (Exception ex)
            {
                info = new ListingInfo();
                info.CurrentStatus = EbayStatus.Unlisted;
                return;
            }
            info = new ListingInfo();
            
            info.ViewUrl = getItem.ApiResponse.Item.ListingDetails.ViewItemURL;
            switch (getItem.ApiResponse.Item.SellingStatus.ListingStatus)
            {
                case ListingStatusCodeType.Active:
                    info.CurrentStatus = EbayStatus.InProgressBuyItNow;
                    break;
                case ListingStatusCodeType.Ended:
                    info.CurrentStatus = EbayStatus.Unsold;
                    break;
                case ListingStatusCodeType.Completed:
                    if (getItem.ApiResponse.Item.SellingStatus.QuantitySold > 0)
                    {
                        info.CurrentStatus = EbayStatus.Sold;
                        info.SoldPrice = getItem.ApiResponse.Item.SellingStatus.CurrentPrice.Value;
                    }
                    else
                    {
                        info.CurrentStatus = EbayStatus.Unsold;
                    }
                    break;
            }
        }
    }
}
