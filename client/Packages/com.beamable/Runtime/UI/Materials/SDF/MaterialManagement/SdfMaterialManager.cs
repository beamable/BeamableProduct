using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.SDF.MaterialManagement {
    public static class SdfMaterialManager {
        private static readonly int BackgroundTexturePropID = Shader.PropertyToID("_BackgroundTexture");
        
        private static readonly Dictionary<SdfMaterialData, Material> _materials =
            new Dictionary<SdfMaterialData, Material>();

        public static Material GetMaterial(Material baseMaterial, Texture secondaryTexture,
            SDFImage.SdfMode imageMode, SdfShadowMode shadowMode, SdfBackgroundMode backgroundMode) {

            var baseMaterialID = baseMaterial.GetInstanceID();
            var data = new SdfMaterialData() {
                baseMaterialID = baseMaterialID,
                secondaryTextureID = secondaryTexture != null ? secondaryTexture.GetInstanceID() : baseMaterialID,
                imageMode = imageMode,
                shadowMode = shadowMode,
                backgroundMode = backgroundMode
            };
            
            if (!_materials.TryGetValue(data, out var material)) {
                material = new Material(baseMaterial);
                _materials[data] = material;
                material.SetTexture(BackgroundTexturePropID, secondaryTexture);
                ApplySdfMode(imageMode, material);
                ApplyShadowMode(shadowMode, material);
                ApplyBackgroundMode(backgroundMode, material);
            }

            return material;
            // TODO: cleaning unused materials
        }

        public static void ApplySdfMode(SDFImage.SdfMode mode, Material material) {
            switch (mode) {
                case SDFImage.SdfMode.Default:
                    material.EnableKeyword("_MODE_DEFAULT");
                    material.DisableKeyword("_MODE_RECT");
                    break;
                case SDFImage.SdfMode.RectOnly:
                    material.DisableKeyword("_MODE_DEFAULT");
                    material.EnableKeyword("_MODE_RECT");
                    break;
            }
        }

        public static void ApplyShadowMode(SdfShadowMode mode, Material material) {
            switch (mode) {
                case SdfShadowMode.Default:
                    material.EnableKeyword("_SHADOWMODE_DEFAULT");
                    material.DisableKeyword("_SHADOWMODE_INNER");
                    break;
                case SdfShadowMode.Inner:
                    material.DisableKeyword("_SHADOWMODE_DEFAULT");
                    material.EnableKeyword("_SHADOWMODE_INNER");
                    break;
            }
        }

        public static void ApplyBackgroundMode(SdfBackgroundMode mode, Material material) {
            switch (mode) {
                case SdfBackgroundMode.Default:
                    material.EnableKeyword("_BGMODE_DEFAULT");
                    material.DisableKeyword("_BGMODE_OUTLINE");
                    material.DisableKeyword("_BGMODE_FULL");
                    break;
                case SdfBackgroundMode.Outline:
                    material.DisableKeyword("_BGMODE_DEFAULT");
                    material.EnableKeyword("_BGMODE_OUTLINE");
                    material.DisableKeyword("_BGMODE_FULL");
                    break;
                case SdfBackgroundMode.Full:
                    material.DisableKeyword("_BGMODE_DEFAULT");
                    material.DisableKeyword("_BGMODE_OUTLINE");
                    material.EnableKeyword("_BGMODE_FULL");
                    break;
            }
        }
    }
}