using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public static class Properties
    {

        public static event GenericEventArgs Saved;

        private static readonly bool Mobile = Application.isMobilePlatform;
        private static readonly bool Web = Application.platform == RuntimePlatform.WebGLPlayer;

        public static int FieldOfView = 60;

        public static float FppMouseSensitivity = 0.5f;
        public static float FppMovementSpeed = 16f;
        public static float FppShiftModifier = 5f;
        public static float FppControlModifier = 0.2f;

        public static float TopMouseSensitivity = 0.2f;
        public static float TopMovementSpeed = 16f;

        public static float IsoMouseSensitivity = 0.2f;
        public static float IsoMovementSpeed = 16f;

        public static WaterQuality WaterQuality = WaterDefaultQuality;

        private static WaterQuality WaterDefaultQuality {
            get {
                if (Mobile || Web)
                {
                    return WaterQuality.SIMPLE;
                }
                else
                {
                    return WaterQuality.HIGH;
                }
            }
        }

        public static void SaveProperties()
        {
            if (Saved != null)
            {
                Saved();
            }
        }

        public static void LoadProperties()
        {

        }

    }
}
