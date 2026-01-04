using HarmonyLib;
using Sons;
using Sons.Gameplay;
using SonsSdk;
using SUI;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Il2CppInterop.Runtime;
using static RedLoader.RLog;
using Il2CppInterop.Runtime.Injection;
using TheForest.Utils;

namespace BetterGolfCarts;

public class BetterGolfCarts : SonsMod
{
    public BetterGolfCarts()
    {

        // Uncomment any of these if you need a method to run on a specific update loop.
        OnUpdateCallback = BetterGolfCartManager.Update;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        HarmonyPatchAll = true;
    }

    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        BetterGolfCartsUi.Create();
        SettingsRegistry.CreateSettings(this, null, typeof(Config));

        // Add in-game settings ui for your mod.
        // SettingsRegistry.CreateSettings(this, null, typeof(Config));
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
    }
}

[HarmonyPatch(typeof(GolfCartController), nameof(GolfCartController.OnEnable))]
public class GolfCartCustomLightPatch
{
    public static void Postfix(GolfCartController __instance)
    {
        Transform lightsGroup = __instance.transform.Find("LightsGroup");
        if (lightsGroup == null) return;

        // 1. Schakel de originele lichten uit (zonder ze te verwijderen)
        // Zo blijven de 'scripts' van de game op de achtergrond draaien zonder visueel effect.
        foreach (var oldLight in lightsGroup.GetComponentsInChildren<Light>(true))
        {
            UnityEngine.Object.Destroy(oldLight);
        }

        // 3. Nieuw licht maken
        GameObject myLightObj = new GameObject("BetterGolfCarts_CustomHeadlight");
        myLightObj.transform.SetParent(lightsGroup, false);

        Light newLight = myLightObj.AddComponent<Light>();
        HDAdditionalLightData hdData = myLightObj.AddComponent<HDAdditionalLightData>();

        // 4. Basis instellingen (Startpunt voor het tunen)
        newLight.type = LightType.Spot;
        newLight.shadows = LightShadows.None;


        hdData.SetSpotAngle(Config.SpotAngle.Value);      // Breedte van de bundel
        hdData.innerSpotPercent = Config.InnerSpotlightPercent.Value;  // Zachtheid van de rand (lager = zachter)
        hdData.shapeRadius = 1f;      // Grootte van de lamp (voor zachte schaduw)
        hdData.SetIntensity(Config.HeadlightIntensity.Value, LightUnit.Lumen);
        hdData.SetRange(200f);
        hdData.UpdateAllLightValues(); //

    }
}

[HarmonyPatch(typeof(GolfCartController), "Start")] // Of Awake
public static class GolfCartController_Start_Patch
{
    [HarmonyPostfix]
    public static void Postfix(GolfCartController __instance)
    {
        BetterGolfCartManager.RegisterCart(__instance);
    }
}

[HarmonyPatch(typeof(PlayerGolfCartDriverAction), nameof(PlayerGolfCartDriverAction.PostInitialize))]
public static class GolfCart_Entry_Patch
{
    [HarmonyPostfix]
    public static void Postfix(GolfCartController __0)
    {
        if (__0 != null)
        {
            BetterGolfCartManager.LocalPlayerCart = __0;
            Msg("LocalPlayerCart succesvol geregistreerd via PlayerGolfCartDriverAction!");
        }
    }
}

[HarmonyPatch(typeof(PlayerGolfCartDriverAction), nameof(PlayerGolfCartDriverAction.TriggerDisconnect))]
public static class GolfCart_Exit_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterGolfCartManager.LocalPlayerCart = null;
        Msg("LocalPlayerCart ontkoppeld.");
    }
}

public class BetterGolfCartManager
{

    public static List<GolfCartController> ActiveCarts = new List<GolfCartController>();

    public static GolfCartController LocalPlayerCart = null;

    private static string testString = "JustForTesting";

    public static void RegisterCart(GolfCartController cart)
    {
        if (cart == null) return;
        if (!ActiveCarts.Contains(cart))
        {
            ActiveCarts.Add(cart);
            // Hier kun je eventueel ook direct je licht-logica aanroepen
        }
    }

    public static void ToggleHighBeams()
    {
            if(LocalPlayerCart == null) return;
            Transform lightsGroup = LocalPlayerCart.transform.Find("LightsGroup");
            if (lightsGroup == null) return;
            var customLight = lightsGroup.Find("BetterGolfCarts_CustomHeadlight");
            if (customLight == null) return;
            var hdData = customLight.GetComponent<HDAdditionalLightData>();
            if (hdData == null) return;
            // Toggle logic
            if (hdData.intensity == Config.HeadlightIntensity.Value)
            {
                // Enable high beams
                hdData.SetIntensity(Config.HeadlightIntensity.Value * 4, LightUnit.Lumen);
                hdData.SetSpotAngle(60f);
            }
            else
            {
                // Disable high beams
                hdData.SetIntensity(Config.HeadlightIntensity.Value, LightUnit.Lumen);
                hdData.SetSpotAngle(Config.SpotAngle.Value);
                hdData.innerSpotPercent = Config.InnerSpotlightPercent.Value;
            }
            hdData.UpdateAllLightValues();
    }

    public static void AdjustLightAngleBasedOnSteering()
    {
        if (LocalPlayerCart == null) return;
        float steeringAngle = LocalPlayerCart._steeringAngle;

        float steeringAngleLightImpactFactor = 0.5f; // Hoeveel invloed het sturen heeft op de lichthoek

        Transform lightsGroup = LocalPlayerCart.transform.Find("LightsGroup");
        if (lightsGroup == null) return;
        var customLight = lightsGroup.Find("BetterGolfCarts_CustomHeadlight");

        if (customLight == null) return;

        // Pas de rotatie van het licht aan op basis van de stuurhoek
        customLight.localRotation = Quaternion.Euler(0, steeringAngle * steeringAngleLightImpactFactor, 0);
    }

    public static void Update()
    {
        AdjustLightAngleBasedOnSteering();
        ActiveCarts.RemoveAll(cart => cart == null);
    }
}