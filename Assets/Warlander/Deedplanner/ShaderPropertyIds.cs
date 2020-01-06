using UnityEngine;

namespace Warlander.Deedplanner
{
    public static class ShaderPropertyIds
    {
        public static readonly int Color = Shader.PropertyToID("_Color");
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    }
}