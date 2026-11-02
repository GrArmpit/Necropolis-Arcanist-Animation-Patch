using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Necro;

[BepInPlugin("githublink?", "Arcanist Animation Patch", "0.2")]
public class ArcanistFix : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    void Awake()
    {
        Log = Logger;
        new Harmony("com.you.necropolis.arcanistfix").PatchAll();
        Log.LogInfo("Arcanist fix loaded");
    }
}

[HarmonyPatch(typeof(Actor), "GetMoveSet")]
static class PatchGetMoveSet
{
    const string ArcanistBody = "arcanist";
    const string DonorBody = "brute";
    static bool inFallback;

    static ActorDef GetDef(Actor actor)
    {
        var t = Traverse.Create(actor);
        ActorDef def = null;
        if (t.Field("actorDef").FieldExists())
            def = t.Field("actorDef").GetValue<ActorDef>();
        else if (t.Property("actorDef").PropertyExists())
            def = t.Property("actorDef").GetValue<ActorDef>();
        return def;
    }

    static void Prefix(Actor __instance, string moveSet, ref Action<MoveSet> callback)
    {
        if (inFallback || callback == null) return;

        ActorDef def = GetDef(__instance);
        if (def == null || def.moveSetBody != ArcanistBody) return;

        Action<MoveSet> original = callback;
        callback = (MoveSet found) =>
        {
            if (found != null) { original(found); return; }

            ArcanistFix.Log.LogInfo("No '" + moveSet + "' for " + ArcanistBody + ", using " + DonorBody);
            string saved = def.moveSetBody;
            inFallback = true;
            try
            {
                def.moveSetBody = DonorBody;
                AccessTools.Method(typeof(Actor), "GetMoveSet")
                    .Invoke(__instance, new object[] { moveSet, original });
            }
            finally
            {
                def.moveSetBody = saved;
                inFallback = false;
            }
        };
    }
}