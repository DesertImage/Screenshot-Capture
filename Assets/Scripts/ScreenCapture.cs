using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Kiddopia.Editor
{
    public class ScreenCapture : ScriptableObject
    {
        public List<Vector2> Resolutions = new List<Vector2>(new[]
        {
            new Vector2(1920, 1080),
            new Vector2(2436, 1125),
            new Vector2(2388, 1668)
        });
    }

    public class ScreenCaptureEditor : EditorWindow
    {
        private const string ResourcesPath = "Assets/Resources/";

        private const string Directory = "Screenshots/Capture/";
        private static string _latestScreenshotPath = "";

        private static ScreenCapture _settings;

        private static List<Vector2> _resolutions;

        private static bool _isInited;

        private static GUIStyle BigText;

        static void Init()
        {
            _isInited = true;

            _settings = Resources.Load<ScreenCapture>("ScreenCaptureSettings");

            if (!_settings)
            {
                _settings = CreateInstance<ScreenCapture>();

                var assetPath = ResourcesPath + "ScreenCaptureSettings.asset";

                AssetDatabase.CreateAsset(_settings, assetPath);
            }

            _resolutions = _settings.Resolutions;

            BigText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            if (!_isInited)
            {
                Init();
            }

            GUILayout.Label("Screen Capture", BigText);
            if (GUILayout.Button("Take a screenshots"))
            {
                TakeScreenshots();
            }

            GUILayout.Space(15);

            GUILayout.Label("Resolutions: ");

            GUILayout.Space(5);

            if ((_resolutions?.Count ?? 0) == 0)
            {
                Init();

                return;
            }

            for (var i = 0; i < _resolutions.Count; i++)
            {
                var resolution = _resolutions[i];

                var x = resolution.x;
                var y = resolution.y;

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    x = EditorGUILayout.IntField((int) resolution.x, GUILayout.Width(50));
                    y = EditorGUILayout.IntField((int) resolution.y, GUILayout.Width(50));

                    _resolutions[i] = new Vector2(x, y);

                    var previousColor = GUI.color;

                    GUI.color = Color.red;

                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        _resolutions.RemoveAt(i);

                        break;
                    }

                    GUI.color = previousColor;

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            var prevColor = GUI.color;

            GUI.color = Color.green;

            if (GUILayout.Button("+", GUILayout.ExpandWidth(true)))
            {
                _resolutions.Add(new Vector2(1920, 1080));
            }

            GUI.color = prevColor;

            GUILayout.Space(16);

            if (GUILayout.Button("Reveal in Explorer"))
            {
                ShowFolder();
            }

            GUILayout.Label("Directory: " + Directory);

            if (GUI.changed && _settings)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        [MenuItem("Tools/Screenshots/Open Window")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(ScreenCaptureEditor));

            window.titleContent.text = "Screenshots settings";
        }

        [MenuItem("Tools/Screenshots/Reveal in Explorer")]
        private static void ShowFolder()
        {
            if (File.Exists(_latestScreenshotPath))
            {
                EditorUtility.RevealInFinder(_latestScreenshotPath);
                return;
            }

            System.IO.Directory.CreateDirectory(Directory);
            EditorUtility.RevealInFinder(Directory);
        }

        [MenuItem("Tools/Screenshots/Take a Screenshot %&s")]
        private static void TakeScreenshots()
        {
            if (!_isInited)
            {
                Init();
            }

            EditorCoroutineUtility.StartCoroutine(ScreenshotProcess(), _settings);
        }

        private static IEnumerator ScreenshotProcess()
        {
            var timeScale = Time.timeScale;

            Time.timeScale = 0f;

            System.IO.Directory.CreateDirectory(Directory);

            var resolutions = new List<Vector2>(_resolutions);

            foreach (var resolution in resolutions)
            {
                GameViewUtils.AddAndSetCustomSize
                (
                    GameViewUtils.GameViewSizeType.FixedResolution,
                    GameViewSizeGroupType.Android,
                    (int) resolution.x,
                    (int) resolution.y,
                    string.Empty
                );

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                // var delay = 0f;
                //
                // while (delay < 1f)
                // {
                //     delay += Time.deltaTime;
                //
                yield return null;
                // }

                var currentTime = DateTime.Now.TimeOfDay;

                var filename =
                    $"{resolution.x}x{resolution.y}__{currentTime.Hours}h{currentTime.Minutes}m{currentTime.Seconds}s.png";

                var path = Directory + filename;

                UnityEngine.ScreenCapture.CaptureScreenshot(path);

                _latestScreenshotPath = path;

                yield return null;
            }

            Time.timeScale = timeScale;
        }
    }
}