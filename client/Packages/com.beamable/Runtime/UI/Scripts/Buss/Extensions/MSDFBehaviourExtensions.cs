using Beamable.UI.MSDF;
using Beamable.UI.Buss.Properties;
using UnityEditor;

namespace Beamable.UI.Buss.Extensions
{
   public static class MSDFBehaviourExtensions
   {
      public static void ApplyStyleObject(this BeamableMSDFBehaviour msdfBehaviour, StyleObject styles)
      {
          if (!msdfBehaviour) return;

          if (styles.Border.IsDefined())
          {
              var border = styles.Border;
              if (border.Width.TryGetValue(styles, out var width))
              {
                  msdfBehaviour.Properties.StrokeErosion = 1 - width;
              }

              if (border.Color.TryGetValue(styles, out var bColor))
              {
                  msdfBehaviour.Properties.StrokeColor = bColor;
              }

              if (border.Radius.TryGetValue(styles, out var radius))
              {
                  msdfBehaviour.Properties.RadiusAmount = radius;
              }
          }

          if (styles.Background.IsDefined())
          {
              var background = styles.Background;

              if (background.Shape.TryGetValue(styles, out var shape))
              {
                  msdfBehaviour.Image.sprite = shape;
              }

              if (background.Color.TryGetValue(styles, out var color))
              {
                  msdfBehaviour.Properties.ForegroundColor = color;
              }

              if (background.Texture.TryGetValue(styles, out var texture))
              {
                  msdfBehaviour.Properties.BackgroundTexture = texture;
              }

              if (background.TextureColor.TryGetValue(styles, out var texColor))
              {
                  msdfBehaviour.Image.color = texColor;
#if UNITY_EDITOR
                  EditorUtility.SetDirty(msdfBehaviour.Image);
#endif
              }

              if (background.TextureScale.TryGetValue(styles, out var texScale))
              {
                  msdfBehaviour.Properties.BackgroundScale = texScale;
              }

              if (background.TextureOffset.TryGetValue(styles, out var texOffset))
              {
                  msdfBehaviour.Properties.BackgroundOffset = texOffset;
              }

              if (background.GradientAmount.TryGetValue(styles, out var gradientAmount))
              {
                  msdfBehaviour.Properties.ForegroundGradientAmount = gradientAmount;
              }

              if (background.GradientEndColor.TryGetValue(styles, out var gradientEndColor))
              {
                  msdfBehaviour.Properties.ForegroundGradientEnd = gradientEndColor;
              }

              if (background.GradientStartColor.TryGetValue(styles, out var gradientStartColor))
              {
                  msdfBehaviour.Properties.ForegroundGradientStart = gradientStartColor;
              }

              if (background.GradientAngle.TryGetValue(styles, out var gradientAngle))
              {
                  msdfBehaviour.Properties.ForegroundGradientDegrees = gradientAngle;
              }

              if (background.GradientScale.TryGetValue(styles, out var gradientScale))
              {
                  msdfBehaviour.Properties.ForegroundGradientScale = gradientScale;
              }

              if (background.GradientOffset.TryGetValue(styles, out var gradientOffset))
              {
                  msdfBehaviour.Properties.ForegroundGradientOffset = gradientOffset;
              }
          }

          msdfBehaviour.RefreshMaterial();

      }
   }
}