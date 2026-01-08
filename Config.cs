using RedLoader;
using RedLoader.Preferences;
using Sons.Gameplay;
using SonsSdk;
using SonsSdk.Attributes;
using SUI;
using System.Runtime.InteropServices;

namespace BetterGolfCarts;

public static class Config
{
    public static ConfigCategory Category { get; private set; }

    public static ConfigEntry<float> SpotAngle { get; private set; }

    public static ConfigEntry<int> InnerSpotlightPercent { get; private set; }

    public static ConfigEntry<float> HeadlightIntensity { get; private set; }

    public static KeybindConfigEntry Highbeams { get; private set; }

    public static KeybindConfigEntry Handbrake { get; private set; }

    public static KeybindConfigEntry Boost { get; private set; }

    public static ConfigEntry<float> BoostMultiplier { get; private set; }





    //public static ConfigEntry<bool> SomeEntry { get; private set; }

    // Auto populated after calling SettingsRegistry.CreateSettings...
    private static SettingsRegistry.SettingsEntry _settingsEntry;

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("BetterGolfCarts", "BetterGolfCarts", "BetterGolfCarts.cfg");

        SpotAngle = Category.CreateEntry("SpotAngleID", 87.9f, "Spot Angle", "The angle of the headlights");
        SpotAngle.SetRange(20f, 110f);

        InnerSpotlightPercent = Category.CreateEntry("InnerSpotID", 30, "Inner spotlight percentage", "Controls the falloff");
        InnerSpotlightPercent.SetRange(0, 100);

        HeadlightIntensity = Category.CreateEntry("HeadlightIntensity", 601124.5f, "Intensity", "Controls the brightness");
        HeadlightIntensity.SetRange(0, 2000000);

        Highbeams = Category.CreateKeybindEntry("HighbeamsKeybind", EInputKey.h, "Highbeams keybind", "Enable the highbeams");
        Highbeams.Notify(BetterGolfCartManager.ToggleHighBeams);

        Handbrake = Category.CreateKeybindEntry("HandbrakeKeybind", EInputKey.p, "Handbrake key bind", "Engage the parking brake");
        Handbrake.Notify(InactiveGolfCartFeaturesPatch.toggleHandbrake);

        Boost = Category.CreateKeybindEntry("BoostKeybind", EInputKey.l, "Boost key bind", "Engage the boost");
        Boost.Notify(InactiveGolfCartFeaturesPatch.toggleBoost);

        BoostMultiplier = Category.CreateEntry("BoostMultiplier", 3f, "Boost strength", "Controls the power of the turbo");
        BoostMultiplier.SetRange(1.0f, 10f);
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
    }
}


