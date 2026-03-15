using UnityEngine;

namespace Warlander.Deedplanner
{
    public static class ShaderPropertyIds
    {
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int Color = Shader.PropertyToID("_Color");  // for SimpleLineShader (Built-in Cg shader)
        public static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    }
}