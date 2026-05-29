using HockeyStickhandling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HockeyStickhandlingEditor
{
    public static class CreatePrototypeScene
    {
        private const string ScenePath = "Assets/HockeyStickhandling/PrototypeScene.unity";

        [MenuItem("Hockey Prototype/Create Prototype Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var bootstrap = new GameObject("Prototype Bootstrap");
            bootstrap.AddComponent<PrototypeBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtility.DisplayDialog(
                "Prototype Scene Created",
                $"Saved {ScenePath}. Add it to Build Settings before building for Quest.",
                "OK");
        }
    }
}
