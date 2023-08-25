using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePart : DynamicModelBehaviour
    {
        public Bridge ParentBridge { get; private set; }

        public Materials Materials => ParentBridge.Data.GetMaterialsForPart(partType, partSide);
        public BridgePartType PartType => partType;

        private BridgePartType partType;
        private BridgePartSide partSide;
        private EntityOrientation orientation;

        private GameObject model;

        public void Initialise(Bridge parentBridge, BridgePartType partType, BridgePartSide partSide,
            EntityOrientation orientation, int x, int y, float height, int skew)
        {
            ParentBridge = parentBridge;
            this.partType = partType;
            this.partSide = partSide;
            this.orientation = orientation;
            
            float finalSkew;
            if (orientation == EntityOrientation.Right || orientation == EntityOrientation.Up)
            {
                finalSkew = -skew;
                transform.position = new Vector3((x + 1) * 4, height * 0.1f + skew * 0.1f, (y + 1) * 4);
            }
            else
            {
                finalSkew = skew;
                transform.position = new Vector3((x + 1) * 4, height * 0.1f, (y + 1) * 4);
            }
            Model rootModel = parentBridge.Data.GetModelForPart(partType, partSide);
            rootModel.CreateOrGetModel(new Vector2(0, finalSkew), OnModelCreated);
        }

        private void OnModelCreated(GameObject newModel)
        {
            if (model)
            {
                Destroy(model);
            }
            
            model = newModel;
            model.transform.SetParent(transform, false);
            
            if (orientation == EntityOrientation.Left)
            {
                model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (orientation == EntityOrientation.Up)
            {
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                model.transform.localPosition = new Vector3(0, 0, -4);
            }
            else if (orientation == EntityOrientation.Right)
            {
                model.transform.localRotation = Quaternion.Euler(0, 270, 0);
                model.transform.localPosition = new Vector3(-4, 0, -4);
            }
            else if (orientation == EntityOrientation.Down)
            {
                model.transform.localPosition = new Vector3(-4, 0, 0);
            }

            OnModelLoadedCallback(model);
        }
    }
}