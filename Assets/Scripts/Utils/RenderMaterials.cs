using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
