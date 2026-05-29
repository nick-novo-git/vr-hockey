using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.OpenXR.Features.OculusQuestSupport;

namespace HockeyStickhandlingEditor
{
    public static class AndroidQuestBuildSetup
    {
        private const string ScenePath = "Assets/HockeyStickhandling/PrototypeScene.unity";
        private const string BuildPath = "Builds/Android/QuestHockeyStickhandling.apk";
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        public static void Configure()
        {
            CreatePrototypeScene();
            ConfigureBuildSettings();
            ConfigureAndroidPlayer();
            ConfigureOpenXR();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildAndroidApk()
        {
            Configure();

            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            }

            Debug.Log($"Android build succeeded: {BuildPath}");
        }

        private static void CreatePrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var bootstrap = new GameObject("Prototype Bootstrap");
            bootstrap.AddComponent<HockeyStickhandling.PrototypeBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        private static void ConfigureAndroidPlayer()
        {
            PlayerSettings.productName = "Quest Hockey Stickhandling";
            PlayerSettings.companyName = "Prototype";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.prototype.questhockeystickhandling");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        }

        private static void ConfigureOpenXR()
        {
            var xrManagerSettings = GetOrCreateAndroidXRManagerSettings();
            xrManagerSettings.automaticLoading = true;
            xrManagerSettings.automaticRunning = true;

            if (!XRPackageMetadataStore.AssignLoader(xrManagerSettings, OpenXRLoaderType, BuildTargetGroup.Android))
            {
                throw new BuildFailedException("Failed to assign OpenXR loader for Android.");
            }

            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            PopulateAndroidOpenXRFeatures(openXrSettings);

            openXrSettings.renderMode = OpenXRSettings.RenderMode.MultiPass;
            openXrSettings.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth24Bit;

            if (!TryEnableOpenXRFeature<MetaQuestFeature>(openXrSettings))
            {
                TryEnableOpenXRFeature<OculusQuestFeature>(openXrSettings);
            }

            TryEnableOpenXRFeature<OculusTouchControllerProfile>(openXrSettings);
            EditorUtility.SetDirty(openXrSettings);
        }

        private static void PopulateAndroidOpenXRFeatures(OpenXRSettings openXrSettings)
        {
            if (openXrSettings.featureCount > 0)
            {
                return;
            }

            var settingsPath = AssetDatabase.GetAssetPath(openXrSettings);
            var androidFeatures = AssetDatabase
                .LoadAllAssetsAtPath(settingsPath)
                .OfType<UnityEngine.XR.OpenXR.Features.OpenXRFeature>()
                .Where(feature => feature.name.EndsWith(" Android"))
                .ToArray();

            var serializedSettings = new SerializedObject(openXrSettings);
            var featuresProperty = serializedSettings.FindProperty("features");
            featuresProperty.arraySize = androidFeatures.Length;
            for (var index = 0; index < androidFeatures.Length; index++)
            {
                featuresProperty.GetArrayElementAtIndex(index).objectReferenceValue = androidFeatures[index];
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(openXrSettings);
        }

        private static XRManagerSettings GetOrCreateAndroidXRManagerSettings()
        {
            const string settingsRoot = "Assets/XR";
            const string settingsFolder = "Assets/XR/Settings";
            const string settingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";

            if (!AssetDatabase.IsValidFolder(settingsRoot))
            {
                AssetDatabase.CreateFolder("Assets", "XR");
            }

            if (!AssetDatabase.IsValidFolder(settingsFolder))
            {
                AssetDatabase.CreateFolder(settingsRoot, "Settings");
            }

            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var buildTargetSettings) ||
                buildTargetSettings == null)
            {
                buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
                if (buildTargetSettings == null)
                {
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(buildTargetSettings, settingsPath);
                }

                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            return buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        private static bool TryEnableOpenXRFeature<TFeature>(OpenXRSettings openXrSettings)
            where TFeature : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var feature = openXrSettings.GetFeature<TFeature>();
            if (feature == null)
            {
                Debug.LogWarning($"OpenXR feature not found in settings: {typeof(TFeature).Name}");
                return false;
            }

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
            return true;
        }
    }
}
