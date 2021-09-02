using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace Beamable.UI.MSDF
{

    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class BeamableMSDFBehaviour : BaseMeshEffect
    {
        public MSDFPropertyCollection Properties = new MSDFPropertyCollection();

        private Image _image;
        private int _lastHash = 0;
        private Material _material;
        private bool _needsRefresh;

        public int _debugMaterialCount = 0;
        public bool _debugForceClean;

        public Image Image => _image == null ? (_image = GetComponent<Image>()) : _image;



        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshMaterial();
        }

        private void Update()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                RefreshMaterial();
            }

            _debugMaterialCount = MSDFMaterialManager.MaterialCount;
            if (_debugForceClean)
            {
                _debugForceClean = false;
                MSDFMaterialManager.Clean(true);
                MSDFMaterialManager.EnsureZeroMaterials();
                _lastHash = 0;
                _material = null;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MSDFMaterialManager.ReleaseMaterial(_lastHash, _material);
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            _needsRefresh = _lastHash != Properties.GetHashCode();
            base.OnValidate();
        }
        #endif


        public override void ModifyMesh(VertexHelper vh)
        {
            UIVertex vert = new UIVertex();

            var center = Vector3.zero;
            var mins = Vector3.one;
            var maxes = Vector3.zero;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                var x = vert.position.x;
                var y = vert.position.y;
                vh.PopulateUIVertex(ref vert, i);
                center += vert.position;
                mins = new Vector2( x < mins.x ? x : mins.x, y < mins.y ? y : mins.y); // don't care about z axis. :/ (UNLESS WE DO!?!? MORE DATA?!?!?)
                maxes = new Vector2( x > maxes.x ? x : maxes.x, y > maxes.y ? y : maxes.y);
            }

            center /= vh.currentVertCount;
            var size = maxes - mins;
//            int topLeftIndex, topRightIndex, lowLeftIndex, lowRightIndex;

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);

                // identify the top-left coordinate
//                if ((vert.position - new Vector3(mins.x, maxes.y)).sqrMagnitude < threshold)
//                {
//                    topLeftIndex = i;
//                }
//                // identify the top-right coordinate
//                if ((vert.position - new Vector3(maxes.x, maxes.y)).sqrMagnitude < threshold)
//                {
//                    topRightIndex = i;
//                }
//                // identify the low-left coordinate
//                if ((vert.position - new Vector3(mins.x, mins.y)).sqrMagnitude < threshold)
//                {
//                    lowLeftIndex = i;
//                }
//                // identify the low-right coordinate
//                if ((vert.position - new Vector3(maxes.x, mins.y)).sqrMagnitude < threshold)
//                {
//                    lowRightIndex = i;
//                }

                // override the sprite renderer color
                //vert.color = Properties.BackgroundColor;

                // set the z-value
                vert.position = new Vector3(vert.position.x, vert.position.y, 1);

                // Map out a uniform UV space over the coordinates.
                var coord = vert.position - mins;
                vert.uv1 = new Vector2(coord.x / size.x, coord.y / size.y);

                // pass along the image aspect ratio, and the container aspect ratio.
                var containerAspect = Image.rectTransform.rect.width / (float) Image.rectTransform.rect.height;
                var requiredAspect = 1f;
                if (Properties != null && Properties.BackgroundTexture != null)
                {
                    requiredAspect = Properties.BackgroundTexture.width / (float)Properties.BackgroundTexture.height;
                }
                vert.uv2 = new Vector2(containerAspect, requiredAspect);

                vh.SetUIVertex(vert, i);
            }

            var growthZ = 0;
            var outterUv = Properties.VertexPadding;
            // add growth vertices

            var outerTopLeft = new UIVertex
            {
                position = center + .5f*new Vector3(-size.x - Properties.VertexPadding.x, size.y + Properties.VertexPadding.y, growthZ),
                uv0 = new Vector2(-outterUv.x, 1 + outterUv.y)
            };
            var outerTopRight = new UIVertex
            {
                position = center + .5f*new Vector3(size.x + Properties.VertexPadding.x, size.y + Properties.VertexPadding.y, growthZ),
                uv0 = new Vector2(1 + outterUv.x, 1 + outterUv.y)

            };
            var outerLowLeft = new UIVertex
            {
                position = center + .5f*new Vector3(-size.x - Properties.VertexPadding.x, -size.y - Properties.VertexPadding.y, growthZ),
                uv0 = new Vector2(-outterUv.x, -outterUv.y)
            };
            var outerLowRight = new UIVertex
            {
                position = center + .5f*new Vector3(size.x + Properties.VertexPadding.x, -size.y - Properties.VertexPadding.y, growthZ),
                uv0 = new Vector2(1 + outterUv.x, -outterUv.y)
            };

            var innerTopLeft = new UIVertex
            {
                position = new Vector3(mins.x, maxes.y, growthZ),
                uv0 = Vector2.up
            };
            var innerTopRight = new UIVertex
            {
                position = new Vector3(maxes.x, maxes.y, growthZ),
                uv0 = Vector2.one
            };
            var innerLowLeft = new UIVertex
            {
                position = new Vector3(mins.x, mins.y, growthZ),
                uv0 = Vector2.zero
            };
            var innerLowRight = new UIVertex
            {
                position = new Vector3(maxes.x, mins.y, growthZ),
                uv0 = Vector2.right
            };

            vh.AddVert(innerTopLeft);
            vh.AddVert(innerTopRight);
            vh.AddVert(innerLowLeft);
            vh.AddVert(innerLowRight);

            vh.AddVert(outerTopLeft);
            vh.AddVert(outerTopRight);
            vh.AddVert(outerLowLeft);
            vh.AddVert(outerLowRight);

            var innerTopLeftIndex = vh.currentVertCount - 8;
            var innerTopRightIndex = vh.currentVertCount - 7;
            var innerLowLeftIndex = vh.currentVertCount - 6;
            var innerLowRightIndex = vh.currentVertCount - 5;
            var outerTopLeftIndex = vh.currentVertCount - 4;
            var outerTopRightIndex = vh.currentVertCount - 3;
            var outerLowLeftIndex = vh.currentVertCount - 2;
            var outerLowRightIndex = vh.currentVertCount - 1;

            // TODO: Revist this. This doesn't create a nice uniform UV space : (
            vh.AddTriangle(outerTopLeftIndex, outerTopRightIndex, innerTopLeftIndex);
            vh.AddTriangle(innerTopLeftIndex, outerTopRightIndex, innerTopRightIndex);

            vh.AddTriangle(outerTopRightIndex, outerLowRightIndex, innerTopRightIndex);
            vh.AddTriangle(innerTopRightIndex, outerLowRightIndex, innerLowRightIndex);

            vh.AddTriangle(outerLowRightIndex, outerLowLeftIndex, innerLowLeftIndex);
            vh.AddTriangle(innerLowLeftIndex, innerLowRightIndex, outerLowRightIndex);

            vh.AddTriangle(outerLowLeftIndex, outerTopLeftIndex, innerTopLeftIndex);
            vh.AddTriangle(innerTopLeftIndex, innerLowLeftIndex, outerLowLeftIndex);

        }

        public void RefreshMaterial()
        {
            var hash = Properties.GetHashCode();
            if (hash == _lastHash) return;

            _material = MSDFMaterialManager.TradeMaterial(hash, _lastHash, _material, Properties);
            if (Image.material != _material)
            {
                Image.material = _material;
            }

            _lastHash = hash;
        }
    }


    [System.Serializable]
    public class MSDFPropertyCollection
    {
        #region Material Properties
        public Color ForegroundColor = Color.white;
        public Color StrokeColor = Color.black;
        [Range(0, 1)]
        public float Threshold = .5f;
        [Range(0, 1)]
        public float StrokeThreshold = .5f;
        [Range(0, 1)]
        public float Softness = 0;
        [Range(0, 1)]
        public float Erosion = 1;
        [Range(0, 1)]
        public float StrokeErosion = 1;
        [Range(0, 1)]
        public float RadiusAmount = 0;

        public Color ForegroundGradientStart = Color.white;
        public Color ForegroundGradientEnd = Color.black;

        [Tooltip("How much should the gradient override existing colors")]
        [Range(0, 1)]
        public float ForegroundGradientAmount = 0;

        [Tooltip("What angle will the gradient be at?")]
        [Range(0, 360)]
        public float ForegroundGradientDegrees = 0;

        [Tooltip("The linear offset for the gradient")]
        [Range(-4, 4)]
        public float ForegroundGradientOffset = 0;

        [Tooltip("The scalar multiplier of the foreground linear gradient dimension")]
        [Range(0, 2)]
        public float ForegroundGradientScale = 1;

        public Texture BackgroundTexture;
        //public Color BackgroundColor;

        [Tooltip("The UV coordinate to pin the left edge of the background texture")]
        [Range(0, 1)]
        public float BackgroundLeftPosition = 0;

        [Tooltip("The UV coordinate to pin the right edge of the background texture")]
        [Range(0, 1)]
        public float BackgroundRightPosition = 1;

        [Tooltip("The UV coordinate to pin the top edge of the background texture")]
        [Range(0, 1)]
        public float BackgroundTopPosition = 1;

        [Tooltip("The UV coordinate to pin the bottom edge of the background texture")]
        [Range(0, 1)]
        public float BackgroundBottomPosition = 0;

        [Tooltip("Should the background image always maintain its aspect ratio?")]
        public bool BackgroundPreserveAspect = false;

        [Tooltip("The tiling coefs for the background. {1,1} is normal. ")]
        public Vector2 BackgroundScale = Vector2.one;

        [Tooltip("The offset coefs for the background. {0,0} is normal. ")]
        public Vector2 BackgroundOffset = Vector2.zero;

        [Range(0, 1)]
        public float DropShadowSoftness = 0;

        public Vector2 DropShadowOffset = Vector2.one;

        public Color DropShadowColor = Color.black;
        #endregion

        #region VBO Data
        public Vector2 VertexPadding = Vector2.zero;
        #endregion

        #region METADATA


        #endregion

        #region DERIVED

        public float BackgroundPreserveAspectFloat => BackgroundPreserveAspect ? 1 : 0;
        public Vector4 BackgroundRect =>
            new Vector4(BackgroundLeftPosition, BackgroundBottomPosition, BackgroundRightPosition,
                BackgroundTopPosition);

        public Vector4 DropShadowData => new Vector4(DropShadowOffset.x, DropShadowOffset.y, DropShadowSoftness, 0);

        #endregion

        private const string DEFAULT_MSDF_ASSET_PATH =
            "Packages/com.beamable/Runtime/UI/Sprites/MSDF/square.png";

        private const float DEG_2_PI = (3.14159f / 180f);


        private static readonly int PropSdfForegroundColor = Shader.PropertyToID("_SDF_ForegroundColor");
        private static readonly int PropSdfStrokeColor = Shader.PropertyToID("_SDF_StrokeColor");
        private static readonly int PropSdfThreshold = Shader.PropertyToID("_SDF_Threshold");
        private static readonly int PropSdfStrokeThreshold = Shader.PropertyToID("_SDF_StrokeThreshold");
        private static readonly int PropSdfSoftness = Shader.PropertyToID("_SDF_Softness");
        private static readonly int PropSdfErosion = Shader.PropertyToID("_SDF_Erosion");
        private static readonly int PropSdfStrokeErosion = Shader.PropertyToID("_SDF_StrokeErosion");
        private static readonly int PropSdfRadiusAmount = Shader.PropertyToID("_SDF_RadiusSize");
        private static readonly int PropSdfDropShadowData = Shader.PropertyToID("_SDF_DropShadowData");
        private static readonly int PropSdfDropShadowColor = Shader.PropertyToID("_SDF_DropShadowColor");
        private static readonly int PropBackgroundTexture = Shader.PropertyToID("_BackgroundTex");
        private static readonly int PropForegroundGradientStart = Shader.PropertyToID("_Foreground_Gradient_Start");
        private static readonly int PropForegroundGradientEnd = Shader.PropertyToID("_Foreground_Gradient_End");
        private static readonly int PropForegroundGradientAngle = Shader.PropertyToID("_Foreground_Gradient_Angle");
        private static readonly int PropForegroundGradientAmount = Shader.PropertyToID("_Foreground_Gradient_Amount");
        private static readonly int PropForegroundGradientOffset = Shader.PropertyToID("_Foreground_Gradient_Offset");
        private static readonly int PropForegroundGradientScale = Shader.PropertyToID("_Foreground_Gradient_Scale");
        private static readonly int PropBackgroundRect = Shader.PropertyToID("_Background_Rect");
        private static readonly int PropBackgroundPreserveAspect = Shader.PropertyToID("_Background_PreserveAspect");


        public void ApplyProperties(Material material)
        {
            material.SetColor(PropSdfForegroundColor, ForegroundColor);
            material.SetColor(PropSdfStrokeColor, StrokeColor);
            material.SetFloat(PropSdfThreshold, Threshold);
            material.SetFloat(PropSdfStrokeThreshold, StrokeThreshold);
            material.SetFloat(PropSdfSoftness, Softness);
            material.SetFloat(PropSdfErosion, Erosion);
            material.SetFloat(PropSdfStrokeErosion, StrokeErosion);
            material.SetFloat(PropSdfRadiusAmount, RadiusAmount);
            material.SetVector(PropSdfDropShadowData, DropShadowData);
            material.SetColor(PropSdfDropShadowColor, DropShadowColor);

            material.SetFloat(PropForegroundGradientAmount, ForegroundGradientAmount);
            material.SetFloat(PropForegroundGradientAngle, ForegroundGradientDegrees * DEG_2_PI);
            material.SetFloat(PropForegroundGradientScale, ForegroundGradientScale);
            material.SetFloat(PropForegroundGradientOffset, ForegroundGradientOffset);
            material.SetColor(PropForegroundGradientStart, ForegroundGradientStart);
            material.SetColor(PropForegroundGradientEnd, ForegroundGradientEnd);

            material.SetVector(PropBackgroundRect, BackgroundRect);
            material.SetTexture(PropBackgroundTexture, BackgroundTexture);
            material.SetTextureScale(PropBackgroundTexture, BackgroundScale);
            material.SetTextureOffset(PropBackgroundTexture, BackgroundOffset);
            material.SetFloat(PropBackgroundPreserveAspect, BackgroundPreserveAspectFloat);
        }

        public override int GetHashCode()
        {
            // XXX: This implementation is not efficient, but I'm prototyping, dawg.
            return GetCodeString().GetHashCode();
        }

        string GetCodeString()
        {
            return $"{BackgroundTexture?.GetHashCode() ?? 0}" +
                   $"_{ForegroundColor}_{StrokeColor}_{Threshold}_{StrokeThreshold}" +
                   $"_{Softness}_{Erosion}_{StrokeErosion}_{RadiusAmount}" +
                   $"_{DropShadowColor}_{DropShadowOffset.GetHashCode()}_{DropShadowSoftness}" +
                   $"_{ForegroundGradientAmount}_{ForegroundGradientDegrees}_{ForegroundGradientOffset}" +
                   $"_{ForegroundGradientStart}_{ForegroundGradientEnd}_{ForegroundGradientScale}" +
                   $"_{BackgroundRect.GetHashCode()}_{BackgroundScale.GetHashCode()}_{BackgroundOffset.GetHashCode()}" +
                   $"_{BackgroundPreserveAspect}";
        }

    }

    public class MSDFMaterialReferenceWrapper
    {
        private Material _material;
        public int ReferenceCount { get; private set; }

        public MSDFMaterialReferenceWrapper(Material material)
        {
            _material = material;
        }

        public Material Checkout()
        {
            ReferenceCount++;
            return _material;
        }

        public void Release()
        {
            ReferenceCount--;
//            if (material == _material)
//            {
//                ReferenceCount--;
//            }
//            else
//            {
//                throw new Exception($"MSDF Material Leak. {material} vs {_material}");
//            }
        }

        public void Cleanup(bool force=false)
        {
            if (!force && ReferenceCount > 0)
            {
                throw new Exception($"Cannot delete material while references are out {_material}");
            }
            #if UNITY_EDITOR
                Object.DestroyImmediate(_material);
            #else
                 Object.Destroy(_material);
            #endif
        }
    }

    public static class MSDFMaterialManager
    {
        private const string MSDF_SHADER_PATH =
            "Packages/com.beamable/Runtime/UI/Materials/BeamableMSDF.shader";

        private const string MSDF_SHADER_NAME = "Unlit/BeamableMSDF";

        private const string PROPERTY_FOREGROUNDCOLOR = "";
        private const string PROPERTY_STROKECOLOR = "_SDF_StrokeColor";

        private static Dictionary<int, MSDFMaterialReferenceWrapper> _hashToMaterial = new Dictionary<int, MSDFMaterialReferenceWrapper>();

        public static int MaterialCount => _hashToMaterial.Count;

        static MSDFMaterialManager()
        {
            Clean(true);
        }

        public static Material TradeMaterial(int nextHash, int lastHash, Material lastMaterial, MSDFPropertyCollection properties)
        {
            if (_hashToMaterial.TryGetValue(lastHash, out var wrapper))
            {
                if (wrapper.ReferenceCount == 1)
                {
                    // I am the only user! I can just edit it!
                    _hashToMaterial.Remove(lastHash);
                    wrapper.Release();

                    if (_hashToMaterial.ContainsKey(nextHash))
                    {
                        wrapper.Cleanup();
                        return _hashToMaterial[nextHash].Checkout();
                    }
                    else
                    {
                        _hashToMaterial.Add(nextHash, wrapper);
                        properties.ApplyProperties(lastMaterial);
                        return wrapper.Checkout();
                    }
                }
                else
                {
                    // there are other people using this, so we need a new material;
                    ReleaseMaterial(lastHash, lastMaterial);
                    return GetMaterial(properties);
                }
            }
            else
            {
                // we don't have any entry for the old hash, so make a new one.
                ReleaseMaterial(lastHash, lastMaterial);
                return GetMaterial(properties);
            }

        }

        public static Material GetMaterial(MSDFPropertyCollection properties)
        {
            var hash = properties.GetHashCode();
            if (_hashToMaterial.TryGetValue(hash, out var existing))
            {
                return existing.Checkout();
            }

            var wrapper = Create(properties);
            _hashToMaterial.Add(hash, wrapper);
            return wrapper.Checkout();
        }

        public static void ReleaseMaterial(int hash, Material material)
        {
            if (_hashToMaterial.TryGetValue(hash, out var wrapper))
            {
                wrapper.Release();
                Clean(); // TODO: Find a better way to call this. This feels pretty bad.
            } else if (material != null)
            {
                Object.DestroyImmediate(material);
            }
        }

        public static void Clean(bool force=false)
        {
            var haveZeroReferences = new List<int>();
            foreach (var kvp in _hashToMaterial)
            {
                if (force || kvp.Value.ReferenceCount == 0)
                {
                    haveZeroReferences.Add(kvp.Key);
                }
            }

            foreach (var hash in haveZeroReferences)
            {
                _hashToMaterial[hash].Cleanup(force);
                _hashToMaterial.Remove(hash);
            }
        }

        public static void EnsureZeroMaterials()
        {
            if (_hashToMaterial.Count > 0)
            {
                throw new Exception($"There are msdf materials not cleaned up. Count={_hashToMaterial.Count}");
            }
        }

        private static MSDFMaterialReferenceWrapper Create(MSDFPropertyCollection properties)
        {
            var material = new Material(Shader.Find(MSDF_SHADER_NAME));
            Debug.Log("Creating material for " + properties.GetHashCode());
            properties.ApplyProperties(material);
            return new MSDFMaterialReferenceWrapper(material);
        }
    }
}