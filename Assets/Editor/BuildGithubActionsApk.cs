using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ZeyWinAds;
using ZeyWinAds.Editor;

public static class BuildGithubActionsApk
{
    private const string DefaultPackageName = "com.playsocialgames.plinko";
    private const string DefaultApiKey = "zw_b805cd36c981a96312521e8d84b20f5b54731a69914c31c0";
    private const string AdMobAppId = "ca-app-pub-6988952582458184~2578339758";
    private const string AdMobBanner = "ca-app-pub-6988952582458184/7966397807";
    private const string AdMobInterstitial = "ca-app-pub-6988952582458184/6653316139";
    private const string AdMobRewarded = "ca-app-pub-6988952582458184/3302497559";

    public static void BuildAndroid()
    {
        ConfigureProject();

        var outputPath = Environment.GetEnvironmentVariable("APK_OUTPUT_PATH");
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.GetFullPath("Builds/Plinko_ZeyWin_v6.apk");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Start.unity", "Assets/Scenes/Game.unity" },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log($"[ZeyWinActions] Build result: {summary.result}, size={summary.totalSize}, apk={outputPath}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception("Android APK build failed: " + summary.result);
    }

    private static void ConfigureProject()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "playsocialgames";
        PlayerSettings.productName = "Plinko";
        var versionName = Environment.GetEnvironmentVariable("ANDROID_VERSION_NAME");
        if (string.IsNullOrEmpty(versionName))
            versionName = "1.0.6";

        var versionCodeText = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE");
        if (!int.TryParse(versionCodeText, out var versionCode))
            versionCode = 6;

        PlayerSettings.bundleVersion = versionName;
        var packageName = Environment.GetEnvironmentVariable("ANDROID_PACKAGE_NAME");
        if (string.IsNullOrEmpty(packageName))
            packageName = DefaultPackageName;

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
        PlayerSettings.Android.bundleVersionCode = versionCode;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        ConfigureKeystore();
        ConfigureZeyWinAds();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureKeystore()
    {
        var keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
        var keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        var keyAlias = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
        var keyAliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");

        if (string.IsNullOrEmpty(keystorePath))
            keystorePath = "/Volumes/Work/ZeyWinSDK/user.keystore";
        if (string.IsNullOrEmpty(keystorePass))
            keystorePass = "12345654321";
        if (string.IsNullOrEmpty(keyAlias))
            keyAlias = "social casino";
        if (string.IsNullOrEmpty(keyAliasPass))
            keyAliasPass = keystorePass;

        if (!File.Exists(keystorePath))
        {
            Debug.LogWarning($"[ZeyWinActions] Keystore not found at {keystorePath}. Building with Unity debug signing.");
            PlayerSettings.Android.useCustomKeystore = false;
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyAliasPass;
    }

    private static void ConfigureZeyWinAds()
    {
        var settings = ZeyWinAdsSettingsEditor.LoadOrCreate();
        var apiKey = Environment.GetEnvironmentVariable("ZEYWIN_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            apiKey = DefaultApiKey;

        settings.apiKey = apiKey;
        settings.autoInitializeOnStartup = true;
        settings.enableAdMob = true;
        settings.enableUmpConsent = false;
        settings.tagForUnderAgeOfConsent = false;
        settings.admobAppIdAndroid = AdMobAppId;
        settings.admobBannerAndroid = AdMobBanner;
        settings.admobInterstitialAndroid = AdMobInterstitial;
        settings.admobRewardedAndroid = AdMobRewarded;
        EditorUtility.SetDirty(settings);

        Debug.Log("[ZeyWinActions] ZeyWin auto-start enabled before splash screen; Unity splash disabled.");
    }
}
