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

        public bool NewListing(bool live, string upc, double price, string[] picFiles, string titleOverride, string description, Shipping shipping, out string response, out string id)
        {
            ApiContext apiContext = GetApiContext(live);

            var addItem = new AddFixedPriceItemCall(apiContext);
            addItem.Item = new ItemType();

            addItem.PictureFileList = new StringCollection();
            addItem.PictureFileList.AddRange(picFiles);
            addItem.Item.PictureDetails = new PictureDetailsType();
            addItem.Item.PictureDetails.GalleryType = GalleryTypeCodeType.Gallery;
            addItem.Item.BestOfferDetails = new BestOfferDetailsType
            {
                BestOfferEnabled = true
            };

            addItem.Item.IncludeRecommendations = true;

            addItem.Item.Title = titleOverride;
            addItem.Item.Description = description;
            addItem.Item.PrimaryCategory = new CategoryType() { CategoryID = "139973" };
            addItem.Item.ConditionID = 4000;
            addItem.Item.ProductListingDetails = new ProductListingDetailsType
            {
                UPC = upc
            };
            addItem.Item.StartPrice = new AmountType() { currencyID = CurrencyCodeType.USD, Value = price };
            addItem.Item.Currency = CurrencyCodeType.USD;
            addItem.Item.Country = CountryCodeType.US;
            addItem.Item.DispatchTimeMax = 1;
            addItem.Item.ListingDuration = "Days_30";
            addItem.Item.ListingType = ListingTypeCodeType.FixedPriceItem;
            addItem.Item.PaymentMethods = new BuyerPaymentMethodCodeTypeCollection { BuyerPaymentMethodCodeType.PayPal };
            addItem.Item.PayPalEmailAddress = paypalEmail;
            addItem.Item.PostalCode = locationZip.ToString();
            addItem.Item.Quantity = 1;
            addItem.Item.ReturnPolicy = new ReturnPolicyType()
            {
                ReturnsAcceptedOption = "ReturnsAccepted",
                RefundOption = "MoneyBack",
                ReturnsWithinOption = "Days_14",
                ShippingCostPaidByOption = "Buyer"
            };

            string shippingMethod = "";
            decimal lbs = 0;
            decimal oz = 0;
            switch (shipping)
            {
                case Shipping.FirstClass:
                    shippingMethod = "USPSFirstClass";
                    oz = 5;
                    break;
                case Shipping.SmallFlatRate:
                    shippingMethod = "USPSPriorityMailSmallFlatRateBox";
                    lbs = 5;
                    break;
                case Shipping.MediumFlatRate:
                    shippingMethod = "USPSPriorityMailFlatRateBox";
                    lbs = 10;
                    break;
                case Shipping.LargeFlatRate:
                    shippingMethod = "USPSPriorityMailLargeFlatRateBox";
                    lbs = 15;
                    break;
                case Shipping.PriorityByWeight:
                    shippingMethod = "USPSPriorityMail";
                    lbs = 2;
                    break;
                default:
                    break;
            }

            addItem.Item.ShippingDetails = new ShippingDetailsType
            {
                ShippingType = ShippingTypeCodeType.Calculated,
                CalculatedShippingRate = new CalculatedShippingRateType
                {
                    OriginatingPostalCode = locationZip.ToString(),
                    PackagingHandlingCosts = new AmountType { currencyID = CurrencyCodeType.USD, Value = 0.0 },
                    ShippingPackage = ShippingPackageCodeType.PackageThickEnvelope,
                    WeightMajor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "lbs", Value = lbs },
                    WeightMinor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "oz", Value = oz }
                },
                ShippingServiceOptions = new ShippingServiceOptionsTypeCollection{
                    new ShippingServiceOptionsType{
                        ShippingService = shippingMethod,
                        ShippingServicePriority = 1,
                    }
                }
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
            public decimal ListPrice { get; set; }
            public EbayStatus CurrentStatus { get; set; }
            public decimal SoldPrice { get; set; }
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
                    info.CurrentStatus = EbayStatus.Sold;
                    break;
            }
        }
    }
}
