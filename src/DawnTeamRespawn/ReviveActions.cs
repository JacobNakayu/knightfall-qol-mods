using Il2Cpp;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using Il2CppPhoton.Pun;

using UnityEngine;

namespace DawnTeamRespawn;

internal static class ReviveActions
{
    // Mimic the sound that plays when you enter an occupied town
    private const HeaderType BannerType = HeaderType.EnterTown_Occupied;
    private const string SplashKey = "DawnTeamRespawn_GetToHorse";

    private static TextSplashProfile _splashProfile;

    public static void ShowBanner(string title, string subtitle)
    {
        HeaderHandler headers = HeaderHandler.instance;
        if (headers == null) return;

        headers.PlayerHeader(BannerType, "");
        headers.headerText.text = title;
        headers.subtitleText.text = subtitle;
    }

    public static void FlashGetToHorse()
    {
        TextSplash splash = TextSplash.instance;
        if (splash == null) return;

        EnsureSplashProfile(splash);
        splash.PlaySplash(SplashKey);
    }

    public static void Revive(Player player)
    {
        var noArgs = new Il2CppReferenceArray<Il2CppSystem.Object>(0);
        player.refs.view.RPC("RPCA_Revive", RpcTarget.All, noArgs);
    }

    public static void ResetWipeSpeedBoost(Player player)
    {
        player.stats.Speed.multiplier = player.stats.Speed.defaultMultiplier;
        player.StatsWereUpdated();
    }

    private static void EnsureSplashProfile(TextSplash splash)
    {
        if (_splashProfile != null && splash.splashes.Contains(_splashProfile)) return;

        _splashProfile = new TextSplashProfile();
        _splashProfile.profileKey = SplashKey;
        _splashProfile.texts = new Il2CppSystem.Collections.Generic.List<string>();
        _splashProfile.texts.Add("MY");
        _splashProfile.texts.Add("HORSE");
        _splashProfile.texts.Add("WILL");
        _splashProfile.texts.Add("SAVE");
        _splashProfile.texts.Add("ME");
        _splashProfile.textColor = Color.white;
        _splashProfile.textTime = 0.25f;

        splash.splashes.Add(_splashProfile);
    }
}