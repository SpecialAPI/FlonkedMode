using BepInEx;
using BrutalAPI;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;

namespace FlonkedMode
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "SpecialAPI.FlonkedMode";
        public const string NAME = "FLONKED Mode";
        public const string VERSION = "1.0.0";

        public static MethodInfo af_i = AccessTools.Method(typeof(Plugin), nameof(AddFlonked_Initialization));
        public static MethodInfo af_pi = AccessTools.Method(typeof(Plugin), nameof(AddFlonked_PostInitialization));

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        public static void AddFlonked_Initialization(EnemyCombat en)
        {
            if (en == null)
                return;

            var pa = Passives.Flonked;

            if (en.TryGetPassiveAbility(pa.m_PassiveID, out var existing))
            {
                en.PassiveAbilities.Remove(existing);
                existing.OnTriggerDettached(en);
            }

            if (en.ContainsPassiveAbility(pa.m_PassiveID))
                return; // WTF?

            en.PassiveAbilities.Add(pa);
            pa.OnTriggerAttached(en);
        }

        public static void AddFlonked_PostInitialization(EnemyCombat en)
        {
            var pa = Passives.Flonked;

            en.TryRemovePassiveAbility(pa.m_PassiveID);
            en.AddPassiveAbility(pa);
        }

        [HarmonyPatch(typeof(EnemyCombat), MethodType.Constructor, typeof(int), typeof(int), typeof(EnemySO), typeof(bool), typeof(int))]
        [HarmonyILManipulator]
        public static void AddFlonked_Enemies_NewEnemy_Transpiler(ILContext ctx)
        {
            var crs = new ILCursor(ctx);

            if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<EnemyCombat>(nameof(EnemyCombat.DefaultPassiveAbilityInitialization))))
                return;

            crs.Emit(OpCodes.Ldarg_0);
            crs.Emit(OpCodes.Call, af_i);
        }

        [HarmonyPatch(typeof(EnemyCombat), nameof(EnemyCombat.TransformEnemy))]
        [HarmonyPostfix]
        public static void AddFlonked_Enemies_PostTransform_Postfix(EnemyCombat __instance)
        {
            AddFlonked_PostInitialization(__instance);
        }

        [HarmonyPatch(typeof(CombatStats), nameof(CombatStats.TryUnboxEnemy))]
        [HarmonyILManipulator]
        public static void AddFlonked_Enemies_Unbox_Transpiler(ILContext ctx)
        {
            var crs = new ILCursor(ctx);

            while (crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<EnemyCombat>(nameof(EnemyCombat.ConnectPassives))))
            {
                crs.Emit(OpCodes.Ldloc_3);
                crs.Emit(OpCodes.Call, af_pi);
            }
        }

        [HarmonyPatch(typeof(WearableStaticModifiers), MethodType.Constructor, [])]
        [HarmonyPostfix]
        public static void AddFlonked_Characters_Postfix_1(WearableStaticModifiers __instance)
        {
            if (__instance._extraPassiveAbilities == null)
                return;

            __instance._extraPassiveAbilities.Insert(0, Passives.Flonked);
        }

        [HarmonyPatch(typeof(WearableStaticModifiers), MethodType.Constructor, typeof(WearableStaticModifiers))]
        [HarmonyPostfix]
        public static void AddFlonked_Characters_Postfix_2(WearableStaticModifiers __instance)
        {
            if (__instance._extraPassiveAbilities == null)
                return;

            __instance._extraPassiveAbilities.Insert(0, Passives.Flonked);
        }
    }
}
