using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public static class RenderMaterials
    {

        public static Material SimpleDrawingMaterial { get; private set; }

        static RenderMaterials()
        {
            Shader drawingShader = Shader.Find("DeedPlanner/SimpleLineShader");
            SimpleDrawingMaterial = new Material(drawingShader);
        }

    }
}
