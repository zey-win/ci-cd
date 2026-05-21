using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using ZeyWinAds;
using ZeyWinAds.Editor;

public static class BuildGithubActionsApk
{
    private const string DefaultPackageName = "com.playsocialgames.plinko";
    private const string DefaultProductName = "Plinko";
    private const string DefaultApiKey = "zw_b805cd36c981a96312521e8d84b20f5b54731a69914c31c0";
    private const string DefaultAdMobAppId = "ca-app-pub-6988952582458184~2578339758";
    private const string DefaultAdMobBanner = "ca-app-pub-6988952582458184/7966397807";
    private const string DefaultAdMobInterstitial = "ca-app-pub-6988952582458184/6653316139";
    private const string DefaultAdMobRewarded = "ca-app-pub-6988952582458184/3302497559";

    public static void BuildAndroid()
    {
        ConfigureProject();

        var outputPath = GetConfig("APK_OUTPUT_PATH", "apkOutputPath", null);
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.GetFullPath("build/android/Plinko_com.playsocialgames.plinko_v6.apk");

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
        var productName = GetConfig("ANDROID_PRODUCT_NAME", "androidProductName", null);
        if (string.IsNullOrEmpty(productName))
            productName = DefaultProductName;

        PlayerSettings.productName = productName;
        var versionName = GetConfig("ANDROID_VERSION_NAME", "androidVersionName", null);
        if (string.IsNullOrEmpty(versionName))
            versionName = "1.0.6";

        var versionCodeText = GetConfig("ANDROID_VERSION_CODE", "androidVersionCode", null);
        if (!int.TryParse(versionCodeText, out var versionCode))
            versionCode = 6;

        PlayerSettings.bundleVersion = versionName;
        var packageName = GetConfig("ANDROID_PACKAGE_NAME", "androidPackageName", null);
        if (string.IsNullOrEmpty(packageName))
            packageName = DefaultPackageName;

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
        PlayerSettings.Android.bundleVersionCode = versionCode;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        ConfigureAndroidCompatibility();

        ConfigureKeystore();
        ConfigureZeyWinAds();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureAndroidCompatibility()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        PlayerSettings.MTRendering = false;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.Android.optimizedFramePacing = false;
        QualitySettings.vSyncCount = 0;

        Debug.Log("[ZeyWinActions] Android compatibility profile: minSdk=23, IL2CPP ARMv7+ARM64, OpenGLES3 only, Vulkan disabled.");
    }

    private static void ConfigureKeystore()
    {
        var keystorePath = GetConfig("ANDROID_KEYSTORE_PATH", "androidKeystorePath", null);
        var keystorePass = GetConfig("ANDROID_KEYSTORE_PASS", "androidKeystorePass", null);
        var keyAlias = GetConfig("ANDROID_KEYALIAS_NAME", "androidKeyaliasName", null);
        var keyAliasPass = GetConfig("ANDROID_KEYALIAS_PASS", "androidKeyaliasPass", null);

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
        var apiKey = GetConfig("ZEYWIN_API_KEY", "zeywinApiKey", null);
        if (string.IsNullOrEmpty(apiKey))
            apiKey = DefaultApiKey;

        settings.apiKey = apiKey;
        settings.autoInitializeOnStartup = true;
        settings.enableAdMob = true;
        settings.enableUmpConsent = false;
        settings.tagForUnderAgeOfConsent = false;
        settings.admobAppIdAndroid = GetConfig("ADMOB_ANDROID_APP_ID", "admobAndroidAppId", DefaultAdMobAppId);
        settings.admobBannerAndroid = GetConfig("ADMOB_ANDROID_BANNER_ID", "admobAndroidBannerId", DefaultAdMobBanner);
        settings.admobInterstitialAndroid = GetConfig("ADMOB_ANDROID_INTERSTITIAL_ID", "admobAndroidInterstitialId", DefaultAdMobInterstitial);
        settings.admobRewardedAndroid = GetConfig("ADMOB_ANDROID_REWARDED_ID", "admobAndroidRewardedId", DefaultAdMobRewarded);
        EditorUtility.SetDirty(settings);

        Debug.Log("[ZeyWinActions] ZeyWin auto-start enabled before splash screen; Unity splash disabled.");
    }

    private static string GetConfig(string envName, string argName, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrEmpty(value))
            return value;

        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-" + argName)
                return args[i + 1];
        }

        return fallback;
    }
}
