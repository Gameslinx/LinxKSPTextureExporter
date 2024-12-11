using HarmonyLib;
using NormalExporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NormalExporter.TextureExporter;

namespace LinxTextureExporter
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Patches : MonoBehaviour
    {
        public static void ModuleManagerPostLoad()
        {
            Debug.Log("Harmony patching texture exporter");
            var harmony = new Harmony("LinxTextureExporter");
            harmony.PatchAll();
        }
    }
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public partial class TextureExporterGUI : MonoBehaviour
    {
        // Update is called once per frame
        private static Rect window = new Rect(100, 100, 450, 600);
        private static Rect windowDefault = new Rect(100, 100, 450, 350);

        static bool showGUI = false;

        private static TextureExporterOptions exportOptions;

        static GUIStyle activeButton;
        public enum GUIEditorMode
        {
            Scatter,
            Terrain,
            Scaled
        }
        void Awake()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                Destroy(this);
            }
        }
        void Start()
        {
            window = new Rect(Screen.width / 2 - 450 / 2, Screen.height / 2 - 50, 450, 100);

            activeButton = new GUIStyle(HighLogic.Skin.button);
            activeButton.normal.textColor = HighLogic.Skin.label.normal.textColor;
            activeButton.hover.textColor = HighLogic.Skin.label.normal.textColor * 1.25f;

            exportOptions = new TextureExporterOptions()
            {
                horizontalResolution = 4096,
                exportColor = true,
                exportHeight = true,
                exportNormal = true,
                multithread = true
            };

            // Register events
            GameEvents.onPlanetariumTargetChanged.Add(OnScaledBodyChanged);
        }
        void OnDisable()
        {
            GameEvents.onPlanetariumTargetChanged.Remove(OnScaledBodyChanged);
        }
        // Update GUI on body change, reset scatter counter and get new scatters
        
        // Test if this body has parallax scaled
        void OnScaledBodyChanged(MapObject body)
        {
            if (body.celestialBody == null)
            {
                // Probably focusing on a vessel. Or at least, not a planet
                return;
            }
        }
        void Update()
        {
            // Determine whether the GUI should be shown or not
            bool toggleDisplayGUI = (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L));
            if (toggleDisplayGUI)
            {
                showGUI = !showGUI;
            }
        }
        void OnGUI()
        {
            // Show GUI if toggle enabled
            if (showGUI)
            {
                window = GUILayout.Window(GetInstanceID(), window, DrawWindow, "Linx's Texture Exporter", HighLogic.Skin.window);
            }
        }
        static void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            ///////////////////////////
            TextureExporterMenu();

            ///////////////////////////
            GUILayout.EndVertical();

            // Must be last or buttons wont work
            UnityEngine.GUI.DragWindow();
        }
        static void TextureExporterMenu()
        {
            if (PlanetariumCamera.fetch.target.type != MapObject.ObjectType.CelestialBody)
            {
                return;
            }

            string planetName = PlanetariumCamera.fetch.target.gameObject.name;
            CelestialBody body = FlightGlobals.GetBodyByName(planetName);

            // Options
            GUILayout.Label("Planet Texture Exporter Options:", HighLogic.Skin.label);

            ParamCreator.CreateParam("Resolution", ref exportOptions.horizontalResolution, GUIHelperFunctions.IntField, null);
            GUILayout.Space(10);
            ParamCreator.CreateParam("Export Height", ref exportOptions.exportHeight, GUIHelperFunctions.BoolField, null);
            ParamCreator.CreateParam("Export Color", ref exportOptions.exportColor, GUIHelperFunctions.BoolField, null);
            ParamCreator.CreateParam("Export Normal", ref exportOptions.exportNormal, GUIHelperFunctions.BoolField, null);
            GUILayout.Space(10);
            ParamCreator.CreateParam("Multithreaded Export", ref exportOptions.multithread, GUIHelperFunctions.BoolField, null);

            if (exportOptions.multithread)
            {
                exportOptions.numThreads = Mathf.Max(SystemInfo.processorCount - 1, 1);
            }
            else
            {
                exportOptions.numThreads = 1;
            }

            if (GUILayout.Button("Export", HighLogic.Skin.button))
            {
                Coroutine co = TextureExporter.Instance.StartCoroutine(TextureExporter.GenerateTextures(exportOptions, body));
            }
            GUILayout.Label("The 'Don't Misclick' Space: ", HighLogic.Skin.label);
            GUILayout.Space(25);
            GUILayout.Label("The Danger Zone: ", HighLogic.Skin.label);
            if (GUILayout.Button("Export Entire System", HighLogic.Skin.button))
            {
                GenerateEntireSystem();
            }
        }
        public static void GenerateEntireSystem()
        {
            Coroutine co = TextureExporter.Instance.StartCoroutine(TextureExporter.GenerateTextures(exportOptions, FlightGlobals.Bodies.ToArray()));
        }
    }
    /// <summary>
    /// Helps create parameters for the in-game parallax config gui.
    /// </summary>
    public static class ParamCreator
    {
        public delegate T ParamTypeMethod<T>(T value, out bool valueChanged);
        public delegate void ChangeMethod();
        /// <summary>
        /// Create a GUI parameter: Left aligned label, right aligned input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="existingValue"></param>
        /// <param name="fieldMethod"></param>
        public static void CreateParam<T>(string name, ref T existingValue, ParamTypeMethod<T> fieldMethod, ChangeMethod callback)
        {
            // Create a left aligned label and right aligned text box
            GUILayout.BeginHorizontal();

            GUILayout.Label(name);
            existingValue = fieldMethod(existingValue, out bool valueWasChanged);

            if (valueWasChanged && callback != null)
            {
                callback();
            }

            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// Create a GUI parameter: Left aligned label, right aligned input. No callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="existingValue"></param>
        /// <param name="fieldMethod"></param>
        /// <returns></returns>
        public static bool CreateParam<T>(string name, ref T existingValue, ParamTypeMethod<T> fieldMethod)
        {
            // Create a left aligned label and right aligned text box
            GUILayout.BeginHorizontal();

            GUILayout.Label(name);
            existingValue = fieldMethod(existingValue, out bool valueWasChanged);

            GUILayout.EndHorizontal();
            return valueWasChanged;
        }
    }
}