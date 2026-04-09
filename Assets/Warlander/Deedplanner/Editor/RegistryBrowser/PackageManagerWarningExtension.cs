using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    [InitializeOnLoad]
    internal class PackageManagerWarningExtension : IPackageManagerExtension
    {
        static PackageManagerWarningExtension()
            => PackageManagerExtensions.RegisterExtension(new PackageManagerWarningExtension());

        private VisualElement _root;

        VisualElement IPackageManagerExtension.CreateExtensionUI()
        {
            _root = new VisualElement();
            _root.style.display = DisplayStyle.None;
            _root.style.flexDirection = FlexDirection.Row;
            _root.style.alignItems = Align.Center;
            _root.style.paddingTop = 6;
            _root.style.paddingBottom = 6;
            _root.style.paddingLeft = 8;
            _root.style.paddingRight = 8;
            _root.style.marginBottom = 8;
            _root.style.borderTopLeftRadius = 3;
            _root.style.borderTopRightRadius = 3;
            _root.style.borderBottomLeftRadius = 3;
            _root.style.borderBottomRightRadius = 3;
            _root.style.borderTopWidth = 1;
            _root.style.borderBottomWidth = 1;
            _root.style.borderLeftWidth = 1;
            _root.style.borderRightWidth = 1;
            _root.style.borderTopColor = new Color(0.7f, 0.55f, 0f);
            _root.style.borderBottomColor = new Color(0.7f, 0.55f, 0f);
            _root.style.borderLeftColor = new Color(0.7f, 0.55f, 0f);
            _root.style.borderRightColor = new Color(0.7f, 0.55f, 0f);
            _root.style.backgroundColor = new Color(0.35f, 0.28f, 0f, 0.35f);

            var icon = new Image();
            icon.image = EditorGUIUtility.IconContent("console.warnicon").image;
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.flexShrink = 0;
            icon.style.marginRight = 8;
            _root.Add(icon);

            var label = new Label(
                "This package is managed by the Registry Browser. " +
                "Use Window \u2192 Registry Browser to install, update, or embed it.");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1;
            _root.Add(label);

            return _root;
        }

        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (_root == null)
                return;

            bool managed = packageInfo != null
                && RegistryBrowserConfig.LoadShowPackageManagerWarning()
                && IsManagedByRegistryBrowser(packageInfo.name);
            _root.style.display = managed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageInfo packageInfo) { }

        void IPackageManagerExtension.OnPackageRemoved(PackageInfo packageInfo) { }

        private static bool IsManagedByRegistryBrowser(string packageName)
        {
            IReadOnlyList<RegistryScope> registries = RegistryBrowserConfig.LoadRegistries();
            foreach (RegistryScope scope in registries)
            {
                if (!string.IsNullOrEmpty(scope.Scope) && packageName.StartsWith(scope.Scope))
                    return true;
            }
            return false;
        }
    }
}
