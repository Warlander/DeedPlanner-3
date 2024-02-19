using System;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Decorations
{
    public class Decoration : FreeformLevelEntity
    {
        private Vector2 position;
        private GameObject model;
        
        public DecorationData Data { get; private set; }
        public override Materials Materials => null;
        public float Rotation { get; private set; }
        
        public override Vector2 Position => position;
        public override bool AlignToSlope => !Data.Floating && !Data.CenterOnly && !Data.CornerOnly && !Data.Tree && !Data.Bush;

        public void Initialize(Tile newTile, DecorationData data, Vector2 newPosition, float newRotation)
        {
            Tile = newTile;
            
            gameObject.layer = LayerMasks.DecorationLayer;

            Data = data;
            position = newPosition;
            Rotation = newRotation;
            
            Data.Model.CreateOrGetModel(OnModelCreated);
        }

        private void OnModelCreated(GameObject newModel)
        {
            if (model)
            {
                Destroy(model);
            }
            
            model = newModel;
            model.transform.SetParent(transform, false);
            model.transform.rotation = Quaternion.Euler(0, Rotation * Mathf.Rad2Deg, 0);

            OnModelLoadedCallback(model);
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", Data.ShortName);
            localRoot.SetAttribute("rotation", Convert.ToString(Rotation, CultureInfo.InvariantCulture));
        }
    }
}