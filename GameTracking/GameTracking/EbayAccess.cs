﻿using eBay.Service.Call;
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

        /// <summary>
        /// Populate eBay SDK ApiContext object with data from application configuration file
        /// </summary>
        /// <returns>ApiContext</returns>
        static ApiContext GetApiContext()
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

                //set Api Server Url
                apiContext.SoapApiServerUrl = "https://api.sandbox.ebay.com/wsapi";
                apiContext.EPSServerUrl = "https://api.sandbox.ebay.com/ws/api.dll";
                //set Api Token to access eBay Api Server
                ApiCredential apiCredential = new ApiCredential();
                using (var sw = new StreamReader("../../../ebayaccess.txt"))
                {
                    apiCredential.eBayToken = sw.ReadLine();
                }
                apiContext.ApiCredential = apiCredential;
                //set eBay Site target to US
                apiContext.Site = SiteCodeType.US;

                return apiContext;
            }
        }

        public string NewListing(string upc, double price, string[] picFiles, string titleOverride, string description, string paypalEmail, int locationZip)
        {
            ApiContext apiContext = GetApiContext();

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

            addItem.Item.ShippingDetails = new ShippingDetailsType
            {
                ShippingType = ShippingTypeCodeType.Calculated,
                CalculatedShippingRate = new CalculatedShippingRateType
                {
                    OriginatingPostalCode = locationZip.ToString(),
                    PackagingHandlingCosts = new AmountType { currencyID = CurrencyCodeType.USD, Value = 0.0 },
                    ShippingPackage = ShippingPackageCodeType.PackageThickEnvelope,
                    WeightMajor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "lbs", Value = 0 },
                    WeightMinor = new MeasureType { measurementSystem = MeasurementSystemCodeType.English, unit = "oz", Value = 5 }
                },
                ShippingServiceOptions = new ShippingServiceOptionsTypeCollection{
                    new ShippingServiceOptionsType{
                        ShippingService = "USPSFirstClass",
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
                return ex.ToString();
            }

            return "http://cgi.sandbox.ebay.com/ws/eBayISAPI.dll?ViewItem&" + addItem.ApiResponse.ItemID;
        }
    }
}
