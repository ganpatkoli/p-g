using UnityEngine;
using UnityEngine.Rendering;

namespace PoolGame.Client.Gameplay
{
    public static class MaterialFactory
    {
        static Shader _lit;

        public static Material Create(Color color, float smoothness = 0.5f)
        {
            if (_lit == null)
            {
                _lit = GraphicsSettings.currentRenderPipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit")
                    : Shader.Find("Standard");
                if (_lit == null) _lit = Shader.Find("Sprites/Default");
            }
            var m = new Material(_lit) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            return m;
        }
    }
}
