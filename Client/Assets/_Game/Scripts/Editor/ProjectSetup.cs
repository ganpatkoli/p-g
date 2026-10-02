using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PoolGame.Client.Editor
{
    /// <summary>One-click project setup: creates a URP asset and makes it the active render pipeline.</summary>
    public static class ProjectSetup
    {
        const string Dir = "Assets/_Game/Settings";

        [MenuItem("PoolGame/Setup URP")]
        public static void SetupUrp()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Game", "Settings");

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, Dir + "/PoolRenderer.asset");
            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(asset, Dir + "/PoolURP.asset");
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = asset;
            Debug.Log("URP configured. Also set Player > Active Input Handling to 'Input System Package (New)' or 'Both'.");
        }
    }
}
