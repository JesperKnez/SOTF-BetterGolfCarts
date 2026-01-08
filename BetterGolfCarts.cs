using HarmonyLib;
using RedLoader;
using PathologicalGames;
using Sons.Gameplay;
using SonsSdk;
using SUI;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using static RedLoader. RLog;

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

        foreach (var oldLight in lightsGroup.GetComponentsInChildren<Light>(true))
        {
            UnityEngine.Object.Destroy(oldLight);
        }

        GameObject myLightObj = new GameObject("BetterGolfCarts_CustomHeadlight");
        myLightObj.transform.SetParent(lightsGroup, false);

        Light newLight = myLightObj.AddComponent<Light>();
        HDAdditionalLightData hdData = myLightObj.AddComponent<HDAdditionalLightData>();

        newLight.type = LightType.Spot;
        newLight.shadows = LightShadows.None;


        hdData.SetSpotAngle(Config.SpotAngle.Value);
        hdData.innerSpotPercent = Config.InnerSpotlightPercent.Value;
        hdData.shapeRadius = 1f;
        hdData.SetIntensity(Config.HeadlightIntensity.Value, LightUnit.Lumen);
        hdData.SetRange(200f);
        hdData.UpdateAllLightValues();

    }
}

[HarmonyPatch(typeof(GolfCartController), "Start")]
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
            BetterGolfCartManager.SetMaxBrakeTorque(__0, Config.MaxBrakeTorque.Value);
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
    }
}

public class BetterGolfCartManager
{

    public static List<GolfCartController> ActiveCarts = new List<GolfCartController>();

    public static GolfCartController LocalPlayerCart = null;

    public static GameObject? CurrentMessageObj = null;

    public static void RegisterCart(GolfCartController cart)
    {
        if (cart == null) return;
        if (!ActiveCarts.Contains(cart))
        {
            ActiveCarts.Add(cart);

            DrawParkingBrakeIcon(cart);
        }
    }

    public static void ToggleHighBeams()
    {
        if (LocalPlayerCart == null) return;
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

        float steeringAngleLightImpactFactor = 0.5f;

        Transform lightsGroup = LocalPlayerCart.transform.Find("LightsGroup");
        if (lightsGroup == null) return;
        var customLight = lightsGroup.Find("BetterGolfCarts_CustomHeadlight");

        if (customLight == null) return;

        customLight.localRotation = Quaternion.Euler(0, steeringAngle * steeringAngleLightImpactFactor, 0);
    }

    public static void SetMaxBrakeTorque(GolfCartController cart, float torque)
    {
        if (cart == null) return;
        cart._definition.MaxBrakeTorque = torque;
    }

    public static void Update()
    {
        AdjustLightAngleBasedOnSteering();
        ActiveCarts.RemoveAll(cart => cart == null);
    }

    public static void DrawParkingBrakeIcon(GolfCartController cart)
    {
        Transform gpsCanvasTransform = cart.transform.Find("GolfCartScreen/GolfCartGps/Canvas");

        if (gpsCanvasTransform != null)
        {
            GameObject redBlock = new GameObject("Handbrake_RedIndicator");

            redBlock.transform.SetParent(gpsCanvasTransform, false);

            redBlock.layer = 5;

            Image img = redBlock.AddComponent<Image>();

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string imagePath = Path.Combine(assemblyDir, "BetterGolfCarts", "handbrake_icon.png");

            if (File.Exists(imagePath))
            {
                img.sprite = LoadSprite(imagePath);
            }
            else 
            {
                Msg($"FOUT: Afbeelding niet gevonden op {imagePath}");
            }
            img.color = Color.white; 

            RectTransform rect = redBlock.GetComponent<RectTransform>();

            rect.sizeDelta = new Vector2(1.5f, 1.5f);

            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);

            rect.pivot = new Vector2(1, 1);

            rect.anchoredPosition = new Vector2(-0.5f, -0.5f);
        }
    }

    public static void ClearMessage(GameObject msgObj)
    {
        if (msgObj == null)
        {
            return;
        }

        try
        {
            if (msgObj.GetComponent<BoostMessageMarker>() != null)
            {
                msgObj.SetActive(false);

                if (PoolManager.Pools.ContainsKey("misc"))
                {
                    PoolManager.Pools["misc"].Despawn(msgObj.transform);
                }

                BetterGolfCartManager.CurrentMessageObj = null;
            }
        }
        catch (System.Exception e)
        {
             RLog.Error("Fout bij verwijderen: " + e.Message);
        }
    }

    public static Sprite LoadSprite(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.filterMode = FilterMode.Bilinear;
        if (ImageConversion.LoadImage(tex, fileData))
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }
}


[HarmonyPatch(typeof(GolfCartController), "ApplyForces", typeof(float), typeof(float), typeof(float), typeof(bool))]
public class InactiveGolfCartFeaturesPatch
{
    private static bool _handbrakeEngaged = false;
    private static bool _boostActive = false;

    public static void toggleHandbrake()
    {
        _handbrakeEngaged = !_handbrakeEngaged;
    }

    public static void toggleBoost()
    {
        _boostActive = !_boostActive;
        if (BetterGolfCartManager.LocalPlayerCart == null) return;

        if (_boostActive)
        {
            SonsTools.ShowMessage("Boost Engaged!", 99999999f);

            return;
        }
        // Alleen verwijderen als we ook daadwerkelijk een bericht-object hebben
        if (BetterGolfCartManager.CurrentMessageObj != null && !_boostActive)
        {
            BetterGolfCartManager.ClearMessage(BetterGolfCartManager.CurrentMessageObj);
        }
    }
    public static void Prefix(GolfCartController __instance, ref bool handBrake)
    {
        handBrake = _handbrakeEngaged;
        if (BetterGolfCartManager.LocalPlayerCart == null) return;

        //BatteryIndicator batteryIndicator = BetterGolfCartManager.LocalPlayerCart.GetComponentInChildren<BatteryIndicator>();

        Transform gpsCanvasTransform = BetterGolfCartManager.LocalPlayerCart.transform.Find("GolfCartScreen/GolfCartGps/Canvas");

        if (gpsCanvasTransform)
        {
            Transform handbrakeIndicatorTransform = gpsCanvasTransform.Find("Handbrake_RedIndicator");

            if (handbrakeIndicatorTransform == null) return;

            GameObject handbrakeIndicatorObj = handbrakeIndicatorTransform.gameObject;

            handbrakeIndicatorObj.SetActive(_handbrakeEngaged);
        }

        //if (_handbrakeEngaged)
        //{
        //    batteryIndicator?.SetSegmentColor(new Color(1f, 0f, 0f, 1f));
        //}
        //else
        //{
        //    batteryIndicator?.SetSegmentColor(new Color(0f, 1f, 0f, 1f)); // should be set to 0 1 0 1 (default from game)
        //}

        __instance.SetBoosting(_boostActive);
        __instance._boostInput = _boostActive;
        __instance._definition.TorqueCurveBoostMultiplier = Config.BoostMultiplier.Value;

    }
}

[RegisterTypeInIl2Cpp]
public class BoostMessageMarker : MonoBehaviour
{
    // Deze klasse fungeert als een marker om berichten van de handrem te identificeren.
}

[HarmonyPatch(typeof(HudGui), "SpawnTimedGameObject")]
public class HudGuiSpawnTimedPatch
{
    public static void Postfix(GameObject go)
    {
        if (go.GetComponent<BoostMessageMarker>() != null) return;
        // Voeg je eigen component toe als uniek kenmerk
        go.AddComponent<BoostMessageMarker>();

        // Sla de referentie op voor later
        BetterGolfCartManager.CurrentMessageObj = go;
    }
}
