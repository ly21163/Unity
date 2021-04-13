using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if mopub_native_beta

// This feature is still in Beta! If you're interested in our Beta Program, please contact support@mopub.com
using NativeAdData = AbstractNativeAd.Data;

#endif

public class MoPubDemoGUI : MonoBehaviour
{
    [Tooltip("Whether to ignore ad unit states and simply enable all buttons to test invalid MoPub API calls " +
             "(e.g., calling Show before Request).")]
    public bool ForceEnableButtons = false;

    public bool completed = false;

    public int counter = 0;


    // State maps to enable/disable GUI ad state buttons
    private readonly Dictionary<string, bool> _adUnitToLoadedMapping = new Dictionary<string, bool>();

    private readonly Dictionary<string, bool> _adUnitToShownMapping = new Dictionary<string, bool>();

    private readonly Dictionary<string, List<MoPub.Reward>> _adUnitToRewardsMapping =
        new Dictionary<string, List<MoPub.Reward>>();

    private bool _consentDialogLoaded;

#if UNITY_IOS
    private readonly string[] _bannerAdUnits =
        { "0ac59b0996d947309c33f59d6676399f", "ef078b27e11c49bbb87080617a69b970", "2aae44d2ab91424d9850870af33e5af7" };

    private readonly string[] _interstitialAdUnits =
        { "4f117153f5c24fa6a3a92b818a5eb630", "9f2859c6726447aa9eaaa43a35ae8682" };

    private readonly string[] _rewardedAdUnits =
        { "8f000bd5e00246de9c789eed39ff6096", "98c29e015e7346bd9c380b1467b33850" };
#elif UNITY_ANDROID || UNITY_EDITOR
    private readonly string[] _bannerAdUnits = { "b195f8dd8ded45fe847ad89ed1d016da" };
    private readonly string[] _interstitialAdUnits = { "24534e1901884e398f1253216226017e" };
    private readonly string[] _rewardedAdUnits =
        { "920b6145fb1546cf8b5cf2ac34638bb7", "a96ae2ef41d44822af45c6328c4e1eb1" };
#endif

#if mopub_native_beta
    private Dictionary<string, MoPubStaticNativeAd> _nativeAds;

    private readonly Dictionary<string, NativeAdData> _adUnitToNativeAdDataMapping =
        new Dictionary<string, NativeAdData>();

    private readonly string[] _nativeAdUnits = {
#if UNITY_EDITOR
        "1"
#elif UNITY_ANDROID
        "11a17b188668469fb0412708c3d16813",
        "09d4e470ca534795b71041c6ca79bac4",
        "05eac26a22974c7f8005878840d4ca5e"
#elif UNITY_IOS

#endif
    };

#endif// mopub_native_beta

    [SerializeField]
    private GUISkin _skin;

    // Label style for no ad unit messages
    private GUIStyle _smallerFont;

    // Buffer space between sections
    private int _sectionMarginSize;

    // Scroll view position
    private Vector2 _scrollPosition;

    // Label style for plugin and SDK version banner
    private GUIStyle _centeredStyle;

    // Default text for custom data fields
    private static string _customDataDefaultText = "Optional custom data";

    // String to fill with custom data for Rewarded ads
    private string _rewardedCustomData = _customDataDefaultText;

    // Flag indicating that personally identifiable information can be collected
    private bool _canCollectPersonalInfo = false;

    // Current consent status of this user to collect personally identifiable information
    private MoPub.Consent.Status _currentConsentStatus = MoPub.Consent.Status.Unknown;

    // Flag indicating that consent should be acquired to collect personally identifiable information
    private bool _shouldShowConsentDialog = false;

    // Flag indicating that the General Data Protection Regulation (GDPR) applies to this user
    private bool? _isGdprApplicable = false;

    // Flag indicating that the General Data Protection Regulation (GDPR) has been forcibly applied by the publisher
    private bool _isGdprForced = false;

    // Status string for tracking current state
    private string _status = string.Empty;

    // Index for current banner ad position, which is incremented after every banner request, starting with BottomCenter
    private int _bannerPositionIndex = 5;

    // All possible banner positions
    private readonly MoPub.AdPosition[] _bannerPositions =
        Enum.GetValues(typeof(MoPub.AdPosition)).Cast<MoPub.AdPosition>().ToArray();


    private static bool IsAdUnitArrayNullOrEmpty(ICollection<string> adUnitArray)
    {
        return (adUnitArray == null || adUnitArray.Count == 0);
    }


    private void AddAdUnitsToStateMaps(IEnumerable<string> adUnits)
    {
        foreach (var adUnit in adUnits) {
            _adUnitToLoadedMapping[adUnit] = false;
            _adUnitToShownMapping[adUnit] = false;
        }
    }


    public void SdkInitialized()
    {
 /*       UpdateConsentValues();*/
    }


    public void UpdateStatusLabel(string message)
    {
        _status = message;
        Debug.Log("Status Label updated to: " + message);
    }


    public void ClearStatusLabel()
    {
        UpdateStatusLabel(string.Empty);
    }


    public void LoadAvailableRewards(string adUnitId, List<MoPub.Reward> availableRewards)
    {
        // Remove any existing available rewards associated with this AdUnit from previous ad requests
        _adUnitToRewardsMapping.Remove(adUnitId);

        if (availableRewards != null) {
            _adUnitToRewardsMapping[adUnitId] = availableRewards;
        }
    }


    public void BannerLoaded(string adUnitId, float height)
    {
        AdLoaded(adUnitId);
        _adUnitToShownMapping[adUnitId] = true;
    }


    public void AdLoaded(string adUnit)
    {
        _adUnitToLoadedMapping[adUnit] = true;
        UpdateStatusLabel("Loaded " + adUnit);
    }


    public void AdDismissed(string adUnit)
    {
        _adUnitToLoadedMapping[adUnit] = false;
        ClearStatusLabel();
    }


    private void Awake()
    {
        if (Screen.width < 960 && Screen.height < 960) {
            _skin.button.fixedHeight = 50;
        }

        _smallerFont = new GUIStyle(_skin.label) { fontSize = _skin.button.fontSize };
        _centeredStyle = new GUIStyle(_skin.label) { alignment = TextAnchor.UpperCenter };

        // Buffer space between sections
        _sectionMarginSize = _skin.label.fontSize;

        AddAdUnitsToStateMaps(_bannerAdUnits);
        AddAdUnitsToStateMaps(_interstitialAdUnits);
        AddAdUnitsToStateMaps(_rewardedAdUnits);
#if mopub_native_beta
        AddAdUnitsToStateMaps(_nativeAdUnits);
#endif
        //ConsentDialogLoaded = false;
    }


    private void Start()
    {
        // The SdkInitialize() call is handled by the MoPubManager prefab now. Please see:
        // https://developers.mopub.com/publishers/unity/initialize/#option-1-initialize-using-the-mopub-manager-recommended

        MoPub.LoadBannerPluginsForAdUnits(_bannerAdUnits);
        MoPub.LoadInterstitialPluginsForAdUnits(_interstitialAdUnits);
        MoPub.LoadRewardedVideoPluginsForAdUnits(_rewardedAdUnits);
#if mopub_native_beta
        MoPub.LoadNativePluginsForAdUnits(_nativeAdUnits);
#endif

#if !(UNITY_ANDROID || UNITY_IOS)
        Debug.LogError("Please switch to either Android or iOS platforms to run sample app!");
#endif

#if UNITY_EDITOR
        Debug.LogWarning("No SDK was loaded since this is not on a mobile device! Real ads will not load.");
#endif

#if mopub_native_beta
        _nativeAds = new Dictionary<string, MoPubStaticNativeAd>();
        var nativeAds = GameObject.Find("MoPubNativeAds");
        if (nativeAds == null) return;
        var staticNativeAds = nativeAds.GetComponentsInChildren<MoPubStaticNativeAd>();
        Debug.Log("Found " + staticNativeAds.Length + " mopub static native ads");
        foreach (var nativeAd in staticNativeAds) {
            _nativeAds[nativeAd.AdUnitId] = nativeAd;
            HideNativeAd(nativeAd);
        }
#else
        var nativeAdsGO = GameObject.Find("MoPubNativeAds");
        if (nativeAdsGO != null)
            nativeAdsGO.SetActive(false);
#endif
    }


    private void Update()
    {
        // Enable scrollview dragging
        foreach (var touch in Input.touches) {
            if (touch.phase != TouchPhase.Moved) continue;
            _scrollPosition.y += touch.deltaPosition.y;
            _scrollPosition.x -= touch.deltaPosition.x;
        }
    }


    private void OnGUI()
    {
        GUI.skin = _skin;

        // Screen.safeArea was added in Unity 2017.2.0p1
        var guiArea = Screen.safeArea;
        guiArea.x += 20;
        guiArea.y += 20;
        guiArea.width -= 40;
        guiArea.height -= 40;
        GUILayout.BeginArea(guiArea);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        CreateTitleSection();

        InitializeMoPubSDK();
 /*     CreateBannersSection();
        CreateInterstitialsSection();
        CreateRewardedVideosSection();
#if mopub_native_beta
        CreateNativeSection();
#endif
        CreateUserConsentSection();
        CreateActionsSection();
        CreateStatusSection();
*/
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    private bool init = false;

    private void CreateTitleSection()
    {
        // App title including Plugin and SDK versions
        var prevFontSize = _centeredStyle.fontSize;
        _centeredStyle.fontSize = 48;
        GUI.Label(new Rect(0, 10, Screen.width, 60), MoPub.PluginName, _centeredStyle);
        _centeredStyle.fontSize = prevFontSize;
        GUI.Label(new Rect(0, 70, Screen.width, 60), "with " + MoPub.SdkName, _centeredStyle);
    }

    private void InitializeMoPubSDK()
    {
        const int titlePadding = 102;
        GUILayout.Space(titlePadding);
        GUILayout.Label("Initialize MoPub SDK");
        //GUILayout.BeginHorizontal();
        GUI.enabled = !init;
        if (GUILayout.Button("Initialize MoPub SDK")) {
            MoPub.InitializeSdk(MoPubManager.Instance.SdkConfiguration); 
            init = true;
        }
        if (init)
        {
            CreateInterstitialsSection();
            CreateRewardedVideosSection();
        }
    }



    private void CreateInterstitialsSection()
    {
        GUILayout.Space(_sectionMarginSize);
        GUILayout.Label("Interstitials");
        if (!IsAdUnitArrayNullOrEmpty(_interstitialAdUnits)) {
            foreach (var interstitialAdUnit in _interstitialAdUnits) {
                GUILayout.BeginHorizontal();

                GUI.enabled = !_adUnitToLoadedMapping[interstitialAdUnit] || ForceEnableButtons;
                if (GUILayout.Button(CreateRequestButtonLabel(interstitialAdUnit))) {
                    Debug.Log("requesting interstitial with AdUnit: " + interstitialAdUnit);
                    UpdateStatusLabel("Load " + interstitialAdUnit);
                    MoPub.RequestInterstitialAd(
                        adUnitId: interstitialAdUnit, keywords: "interstitial, mopub");
                }

                GUI.enabled = _adUnitToLoadedMapping[interstitialAdUnit] || ForceEnableButtons;
                if (GUILayout.Button("Show")) {
                    ClearStatusLabel();
                    MoPub.ShowInterstitialAd(interstitialAdUnit);
                    if (completed) { Debug.Log("Completed watching"); }
                    else { Debug.Log("Not completed watching"); }
                }
/*
                GUI.enabled = _adUnitToLoadedMapping[interstitialAdUnit] || ForceEnableButtons;
                if (GUILayout.Button("Destroy")) {
                    ClearStatusLabel();
                    MoPub.DestroyInterstitialAd(interstitialAdUnit);
                    _adUnitToLoadedMapping[interstitialAdUnit] = false;
                }
*/
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        } else {
            GUILayout.Label("No interstitial AdUnits available", _smallerFont, null);
        }
    }


    private void CreateRewardedVideosSection()
    {
        GUILayout.Space(_sectionMarginSize);
        GUILayout.Label("Rewarded Videos");
        if (!IsAdUnitArrayNullOrEmpty(_rewardedAdUnits)) {
            //CreateCustomDataField("rewardedCustomDataField", ref _rewardedCustomData);
            foreach (var rewardedAdUnit in _rewardedAdUnits) {
                GUILayout.BeginHorizontal();

                GUI.enabled = !_adUnitToLoadedMapping[rewardedAdUnit] || ForceEnableButtons;
                if (GUILayout.Button(CreateRequestButtonLabel(rewardedAdUnit))) {
                    Debug.Log("requesting rewarded ad with AdUnit: " + rewardedAdUnit);
                    UpdateStatusLabel("Load " + rewardedAdUnit);
                    MoPub.RequestRewardedVideo(
                        adUnitId: rewardedAdUnit, keywords: "rewarded, video, mopub",
                        latitude: 37.7833, longitude: 122.4167, customerId: "customer101");
                }

                GUI.enabled = _adUnitToLoadedMapping[rewardedAdUnit] || ForceEnableButtons;
                if (GUILayout.Button("Show")) {
                    ClearStatusLabel();
                    MoPub.ShowRewardedVideo(rewardedAdUnit, GetCustomData(_rewardedCustomData));
                    Debug.Log("Finished watching times: " + counter);
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();


                // Display rewards if there's a rewarded video loaded and there are multiple rewards available
                if (!MoPub.HasRewardedVideo(rewardedAdUnit)
                    || !_adUnitToRewardsMapping.ContainsKey(rewardedAdUnit)
                    || _adUnitToRewardsMapping[rewardedAdUnit].Count <= 1) continue;

                GUILayout.BeginVertical();
                GUILayout.Space(_sectionMarginSize);
                GUILayout.Label("Select a reward:");

                foreach (var reward in _adUnitToRewardsMapping[rewardedAdUnit]) {
                    if (GUILayout.Button(reward.ToString())) {
                        MoPub.SelectReward(rewardedAdUnit, reward);
                    }
                }

                GUILayout.Space(_sectionMarginSize);
                GUILayout.EndVertical();
            }
        } else {
            GUILayout.Label("No rewarded AdUnits available", _smallerFont, null);
        }
    }



    private static string GetCustomData(string customDataFieldValue)
    {
        return customDataFieldValue != _customDataDefaultText ? customDataFieldValue : null;
    }


    private static string CreateRequestButtonLabel(string adUnit)
    {
        return adUnit.Length > 10 ? "Load " + adUnit.Substring(0, 10) + "..." : adUnit;
    }
}
