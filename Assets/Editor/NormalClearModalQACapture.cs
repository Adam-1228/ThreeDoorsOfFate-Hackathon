using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Editor
{
    public static class NormalClearModalQACapture
    {
        private const string ScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        public static void Capture()
        {
            string outputPath = Environment.GetEnvironmentVariable("TDOF_QA_CAPTURE_OUTPUT");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "../../Builds/QA/normal_clear_modal_fixed.png"));
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new InvalidOperationException($"Invalid QA capture output path: {outputPath}");
            }

            Directory.CreateDirectory(outputDirectory);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Type controllerType = Type.GetType(ControllerTypeName)
                ?? throw new InvalidOperationException("ThreeDoorsGameController could not be loaded.");
            Component controller = Resources.FindObjectsOfTypeAll(controllerType)
                .OfType<Component>()
                .FirstOrDefault(candidate => candidate.gameObject.scene == scene)
                ?? throw new InvalidOperationException("ThreeDoorsGameController was not found in the playable scene.");

            if (GetField<RectTransform>(controllerType, controller, "contentRoot") == null)
            {
                Invoke(controllerType, controller, "BuildShell");
            }

            SetEnumField(controllerType, controller, "currentDifficulty", "Normal");
            SetField(controllerType, controller, "debt", 0);
            SetField(controllerType, controller, "gold", 94);
            Invoke(controllerType, controller, "ShowTenDoorClearChoice");

            Canvas canvas = GetField<Canvas>(controllerType, controller, "canvas")
                ?? throw new InvalidOperationException("Runtime canvas was not created.");
            RenderCanvas(canvas, outputPath);
            Debug.Log($"Normal-clear modal QA capture written: {outputPath}");
        }

        private static void RenderCanvas(Canvas canvas, string outputPath)
        {
            GameObject cameraObject = new("Normal Clear QA Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            RenderTexture target = new(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            Texture2D capture = new(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;

            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = target;

                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                capture.Apply(false, false);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void Invoke(Type controllerType, Component controller, string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Controller method was not found: {methodName}");
            method.Invoke(controller, null);
        }

        private static T GetField<T>(Type controllerType, Component controller, string fieldName)
            where T : class
        {
            return GetFieldInfo(controllerType, fieldName).GetValue(controller) as T;
        }

        private static void SetField(
            Type controllerType,
            Component controller,
            string fieldName,
            object value)
        {
            GetFieldInfo(controllerType, fieldName).SetValue(controller, value);
        }

        private static void SetEnumField(
            Type controllerType,
            Component controller,
            string fieldName,
            string value)
        {
            FieldInfo field = GetFieldInfo(controllerType, fieldName);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static FieldInfo GetFieldInfo(Type controllerType, string fieldName)
        {
            return controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Controller field was not found: {fieldName}");
        }
    }
}
