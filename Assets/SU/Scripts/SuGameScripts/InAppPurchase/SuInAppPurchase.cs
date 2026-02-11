//using Firebase.Crashlytics;
using System;
//using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

using System.Linq;
using System.Collections.ObjectModel;



//using Unity.Services.Core;
//using Unity.Services.Core.Environments;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#endif
using Sirenix.OdinInspector;


public class SuInAppPurchase : BaseSUUnit
{
    //public static Action<IAPProductIDName> OnInitializeSuccess;
    public static Action OnPurchaseSuccess;
    public static Dictionary<IAPProductIDName, IAPProductModule> ProductsDict;
    public static Action onTimeOutBundlePurchased;

    StoreController m_StoreController;

    public static bool isInitialized = false;
    public List<IAPProductModule> productsList;
    public override void Init(bool test)
    {
        if(test)
        {
            isTest = test;
            //StandardPurchasingModule.Instance().useFakeStoreAlways = true;
        }
        InitializeIAP();
    }
     


    private bool isConnected = false;

    async void InitializeIAP()
    {
        isInitialized = true;
        ConfigProductDict();

        m_StoreController = UnityIAPServices.StoreController();

        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnProductsFetched += OnProductsFetched;
        m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
        m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
        m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

        await m_StoreController.Connect();

        isConnected = true;
        LogManager.Log("[IAP]IAP initialized successfully.");
        

        var initialProductsToFetch = new List<ProductDefinition>();
        foreach (IAPProductModule _product in productsList)
        {
            initialProductsToFetch.Add(new ProductDefinition(_product.productId, _product.productType));
        }

        m_StoreController.ProcessPendingOrdersOnPurchasesFetched(false);
        m_StoreController.FetchProducts(initialProductsToFetch);

        
    }

    private IAPProductModule GetProductModule(string productId)
    {
        return productsList.Find(x => x.productId == productId);
    }
    void OnPurchaseConfirmed(Order order)
    {
        var product = GetFirstProductInOrder(order);
        //if (product == null)
        //{
        //    LogManager.Log("[IAP]Could not find product in purchase confirmation.");
        //}
        IAPProductModule productModule = GetProductModule(GetIdFromProduct(product));

        switch (order)
        {
            case ConfirmedOrder:
                //LogManager.Log($"[IAP]Order confirmed - Product: {GetIdFromProduct(product)}");
                OnPurchaseSuccessed(productModule);
                break;
            case FailedOrder:
                //LogManager.Log($"[IAP]Confirmation failed - Product: {GetIdFromProduct(product)}");
                break;
            default:
                //LogManager.Log("[IAP]Unknown OnPurchaseConfirmed result.");
                break;
        }
    }
    void OnStoreDisconnected(StoreConnectionFailureDescription failureDescription)
    {
        LogManager.Log("[IAP]Store disconnected: " + failureDescription.message);
        isInitialized = false;
    }
    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription purchasesFetchFailed)
    {
        LogManager.Log("[IAP]Failed to fetch purchases: " + purchasesFetchFailed.FailureReason);
        //isInitialized = false;
    }
    void OnProductsFetchFailed(ProductFetchFailed productFetchFailed)
    {
        LogManager.Log("[IAP]Failed to fetch products: " + productFetchFailed.FailureReason);
        isInitialized = false;
    }
    void OnProductsFetched(List<Product> products)
    {
        // Handle fetched products  
        //Debug.Log("[IAP]Products fetched: " + products.Count);
        m_StoreController.FetchPurchases();
        
        foreach (var product in products)
        {
            //LogManager.Log("[IAP]Fetched product: " + product.definition.id + ", price: " + product.metadata.localizedPriceString);
            for (int i = 0; i < productsList.Count; i++)
            {
                IAPProductModule _product = productsList[i];
                if (_product.productId == product.definition.id)
                {
                    // set real price and product for IAPProduct 
                    _product.price = product.metadata.isoCurrencyCode + " " + product.metadata.localizedPrice.ToString();// product.metadata.localizedPriceString;
                    _product.priceNumber = product.metadata.localizedPrice;
                    _product.product = product;
                    //Debug.Log($"[IAP]Product {_product.ProductName} initialized with price: {_product.price} and ID: {_product.productId}");
                }
            }
        }
    }
    void OnPurchasesFetched(Orders orders)
    {
        // Process purchases, e.g. check for entitlements from completed orders  
        
    }

    void OnPurchasePending(PendingOrder order)
    {
        string receipt = order.Info.Receipt;
        string id = GetIdFromProduct(GetFirstProductInOrder(order));
        // Add your validations here before confirming the purchase.
        foreach (IAPProductModule _product in productsList)
        {
            if (_product.productId == id)
            {
                bool validPurchase = true;
                if(isTest == false)
                {
                    try
                    {
                        //var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
                        //// On Google Play, result has a single product ID.
                        //// On Apple stores, receipts contain multiple products.
                        //var result = validator.Validate(receipt);



                        // For informational purposes, we list the receipt(s)
                        //Debug.Log("Receipt is valid. Contents:");
                        //foreach (IPurchaseReceipt productReceipt in result)
                        //{
                        //    LogManager.Log(productReceipt.productID);
                        //    LogManager.Log(productReceipt.purchaseDate);
                        //    LogManager.Log(productReceipt.transactionID);
                        //}

                    }
                    catch (IAPSecurityException eee)
                    {
                        LogManager.Log("[IAP]Invalid receipt, not unlocking content " + eee);
                        validPurchase = false;
                    }
                }    
                
#if UNITY_EDITOR
                //editor thì luôn set = true để test
                validPurchase = true;
#endif
                if (isTest)
                {
                    validPurchase = true;
                }
                if (validPurchase)
                {
                    m_StoreController.ConfirmPurchase(order);
                    //LogManager.Log("Buy success product " + _product.product.metadata.localizedPrice);
                }
                else
                {
                    SuGame.Get<SuAnalytics>().LogEvent(EventName.Purchase_Fail, new Param(ParaName.ID, _product.productId), new Param(ParaName.Reason, "invalid_receipt"));
                }
                break;

            }
        }

    }

    private void ConfigProductDict()
    {
        ProductsDict = new Dictionary<IAPProductIDName, IAPProductModule>();
        for (int i = 0; i < productsList.Count; i++)
        {
            IAPProductModule md = productsList[i];
            if (!ProductsDict.ContainsKey(md.ProductName))
            {
                ProductsDict.Add(md.ProductName, md);
            }
        }
    }    
    
    public void OnPurchaseFailed(FailedOrder failedOrder)
    {
        PopupManager.OpenPopup(PopupType.PurchaseFail);
        string productId = GetIdFromProduct(GetFirstProductInOrder(failedOrder));
        SuGame.Get<SuAnalytics>().LogEvent(EventName.Purchase_Fail, new Param(ParaName.ID, productId), new Param(ParaName.Reason, failedOrder.FailureReason.ToString()));
        LogManager.Log("[IAP]Purchase failed for product: " + productId + ", reason: " + failedOrder.FailureReason);
        OnPurchaseSuccess = null;
    }
    
    
    public string GetLocalizePrice(IAPProductIDName bundleName)
    {
        for(int i = 0; i < productsList.Count; i++)
        {
            if(productsList[i].ProductName == bundleName)
            {
                return productsList[i].price;
            }
        }
        return "";
    }


    public Product FindProduct(string productId)
    {
        return GetFetchedProducts()?.FirstOrDefault(product => product.definition.id == productId);
    }

    public ReadOnlyObservableCollection<Product> GetFetchedProducts()
    {
        return m_StoreController.GetProducts();
    }


    public void BuyProduct(IAPProductIDName name, string category, Action onPurchaseSuccess = null)
    {
        if (name == IAPProductIDName.Free)
            return;
        if (isInitialized == false)
        {
            //Debug.Log("[IAP]IAP is not initialized yet. Please wait for initialization to complete.");
            return;
        }
        if (isConnected == false)
        {
            //Debug.Log("[IAP]Not connected to the store. Please check your internet connection.");
            return;
        }

        OnPurchaseSuccess = onPurchaseSuccess;

//#if !UNITY_EDITOR
//        if (isTest)
//        {
//            LogManager.Log($"[IAP]Test mode is enabled. Simulating purchase for product: {name}");
//            // Simulate a successful purchase in test mode
//            IAPProductModule productModule = productsList.Find(x => x.ProductName == name);
//            if (productModule == null)
//            {
//                LogManager.Log($"[IAP]No product module found for product: {name}");
//                return;
//            }
//            Debug.Log($"[IAP]Simulating purchase for product: {productModule.ProductName}, ID: {productModule.productId}, Price: {productModule.price}");
//            Debug.Log(productModule.product == null ? "[IAP]Product is null" : $"[IAP]Product is not null, ID: {productModule.product.definition.id}, Price: {productModule.product.metadata.localizedPriceString}");
//            if (productModule != null)
//            {
//                OnPurchaseSuccessed(productModule);
//            }
//            return;
//        }
//#endif



        IAPProductModule prd = productsList.Find(x => x.ProductName == name);
        if (prd != null)
        {
            var product = prd.product;

            if (product != null)
            {
                SuGame.Get<SuAds>().LockAppOpenAds = true;
                actionEarn = name.ToString();
                categoryEarn = category;
                m_StoreController.PurchaseProduct(product);
            }
            else
            {
                //Debug.Log($"[IAP]The product {name} has no product");
            }
        }
        else
        {
               // Debug.Log($"[IAP]No product found with name: {name}");
        }
    }




    private static string actionEarn;
    private static string categoryEarn;
    public void OnPurchaseSuccessed(IAPProductModule _product)
    {
        if (_product.ProductName == IAPProductIDName.timeout_bundle)
            onTimeOutBundlePurchased?.Invoke();
        OnPurchaseSuccess?.Invoke();
        //ShopManager.ProcessPurchaseSuccess(_product.ProductName,actionEarn,categoryEarn);

        SuGame.Get<SuAnalytics>().LogEvent(EventName.Purchase_Success, new Param(ParaName.ID, _product.productId));
        SuGame.Get<SuAnalytics>().LogEventIAP(_product.product.metadata.localizedPrice, _product.product.metadata.isoCurrencyCode, SuLevelManager.CurrentLevel, _product.ProductName.ToString());
        SuGame.Get<SuAdjust>().LogEvent(EventName.inapp_purchase, (double)_product.product.metadata.localizedPrice, _product.product.metadata.isoCurrencyCode);
        SuGame.Get<SuAnalytics>().LogEventPurchase(_product.product.metadata.localizedPrice, _product.product.metadata.isoCurrencyCode);
    }
    public void RestorePurchases()
    {
        m_StoreController.RestoreTransactions(OnTransactionsRestored);
    }
    void OnTransactionsRestored(bool success, string error)
    {
        LogManager.Log("[IAP]Transactions restored: " + success);
    }

    Product GetFirstProductInOrder(Order order)
    {
        return order.CartOrdered.Items().FirstOrDefault()?.Product;
    }
    string GetIdFromProduct(Product product)
    {
        return product?.definition.id ?? "[IAP]no product found";
    }
}

[Serializable]
public class IAPProductModule
{
    //#if SUGAME_VALIDATED
    [EnumPaging]
    public IAPProductIDName ProductName;
    public string productId
    {
        get
        {
            // quy tắc đặt tên product là packageName.productName
            // ví dụ com.sg.blockpuzzle.noads
            return Application.identifier + "." + ProductName;
        }
    }
    [EnumPaging]
    public ProductType productType;
    [HideInInspector]
    public decimal priceNumber;
    //[LabelText("Default Price")]
    public string price;
    [HideInInspector]
    public Product product;
    [HideInInspector]
    public SubscriptionInfo subsInfo;
    //#endif
}

[System.Serializable]
public enum IAPProductIDName
{
    startbundle,
    minibundle,
    largebundle,
    ultrabundle,
    giantbundle,
    legendarybundle,
    supremebundle,
    noads,
    ticket_10,
    ticket_20,
    ticket_50,
    ticket_100,
    ticket_400,
    gold_1k,
    gold_5k,
    gold_10k,
    gold_25k,
    gold_50k,
    gold_100k,
    noads24h,
    timeout_bundle,
    treasure_1,
    treasure_2,
    treasure_3,
    treasure_4,
    treasure_5,
    treasure_6,
    treasure_7,
    treasure_8,
    treasure_9,
    treasure_10, 
    Free,
    heart_offer,
    rope_premium_1,
    rope_premium_2,
    rope_premium_3,
    pin_premium_1,
    pin_premium_2,
    pin_premium_3,
    //halloween items:
    rope_halloween_1,
    rope_halloween_2,
    rope_halloween_3,
    special_offer,
    pin_xmas_1,
    pin_xmas_2,
    pin_xmas_3,
    pin_xmas_4,
    background_xmas_1,
    background_xmas_2,
    background_xmas_3,
    background_xmas_4,
    rope_xmas_1,
    rope_xmas_2,
    rope_xmas_3,
    battlepass_vip
}


