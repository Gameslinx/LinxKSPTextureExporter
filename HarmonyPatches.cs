using CommNet;
using HarmonyLib;
using NormalExporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LinxTextureExporter
{
    [HarmonyPatch(typeof(PQS))]
    [HarmonyPatch("DestroyQuad", typeof(PQ))]
    public class HarmonyPatches
    {
        public static bool Prefix(PQS __instance, PQ quad)
        {
            if (TextureExporter.planetExportProgress > 0)
            {
                Debug.Log("Skipped quad destroy");
                return false;
            }
            if (quad == null)
            {
                return false;
            }
            else
            {
                return true;
            }
            //__instance.Mod_OnQuadDestroy(quad);
            //if (quad.isBuilt)
            //{
            //    // fucking nothing
            //}
            //__instance.cache.DestroyQuad(quad);
        }
    }

    [HarmonyPatch(typeof(CommNetHome))]
    [HarmonyPatch("CreateNode")]
    public class HarmonyPatches2
    {
        public static bool Prefix(CommNetHome __instance)
        {
            if (TextureExporter.planetExportProgress > 0)
            {
                Debug.Log("Skipped commnet stuff");
                __instance.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(__instance);
                return false;
            }
            return true;
            //__instance.Mod_OnQuadDestroy(quad);
            //if (quad.isBuilt)
            //{
            //    // fucking nothing
            //}
            //__instance.cache.DestroyQuad(quad);
        }
    }
}
