// Decompiled with JetBrains decompiler
// Type: UnityEngine.ColorUsageAttribute
// Assembly: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 606BEDCE-C7FF-4C91-B20E-0BADE6B79408
// Assembly location: C:\Program Files\Unity_18\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll

using System;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
  /// <summary>
  ///   <para>Attribute used to configure the usage of the ColorField and Color Picker for a color.</para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public sealed class ColorUsageAttribute : PropertyAttribute
  {
    /// <summary>
    ///   <para>If false then the alpha bar is hidden in the ColorField and the alpha value is not shown in the Color Picker.</para>
    /// </summary>
    public readonly bool showAlpha = true;
    /// <summary>
    ///   <para>If set to true the Color is treated as a HDR color.</para>
    /// </summary>
    public readonly bool hdr = false;
    /// <summary>
    ///   <para>Minimum allowed HDR color component value when using the Color Picker.</para>
    /// </summary>
    [Obsolete("This field is no longer used for anything.")]
    public readonly float minBrightness = 0.0f;
    /// <summary>
    ///   <para>Maximum allowed HDR color component value when using the HDR Color Picker.</para>
    /// </summary>
    [Obsolete("This field is no longer used for anything.")]
    public readonly float maxBrightness = 8f;
    /// <summary>
    ///   <para>Minimum exposure value allowed in the HDR Color Picker.</para>
    /// </summary>
    [Obsolete("This field is no longer used for anything.")]
    public readonly float minExposureValue = 0.125f;
    /// <summary>
    ///   <para>Maximum exposure value allowed in the HDR Color Picker.</para>
    /// </summary>
    [Obsolete("This field is no longer used for anything.")]
    public readonly float maxExposureValue = 3f;

    /// <summary>
    ///   <para>Attribute for Color fields. Used for configuring the GUI for the color.</para>
    /// </summary>
    /// <param name="showAlpha">If false then the alpha channel info is hidden both in the ColorField and in the Color Picker.</param>
    public ColorUsageAttribute(bool showAlpha)
    {
      this.showAlpha = showAlpha;
    }

    /// <summary>
    ///   <para>Attribute for Color fields. Used for configuring the GUI for the color.</para>
    /// </summary>
    /// <param name="showAlpha">If false then the alpha channel info is hidden both in the ColorField and in the Color Picker.</param>
    /// <param name="hdr">Set to true if the color should be treated as a HDR color (default value: false).</param>
    public ColorUsageAttribute(bool showAlpha, bool hdr)
    {
      this.showAlpha = showAlpha;
      this.hdr = hdr;
    }

    /// <summary>
    ///   <para>Attribute for Color fields. Used for configuring the GUI for the color.</para>
    /// </summary>
    /// <param name="showAlpha">If false then the alpha channel info is hidden both in the ColorField and in the Color Picker.</param>
    /// <param name="hdr">Set to true if the color should be treated as a HDR color (default value: false).</param>
    /// <param name="minBrightness">Minimum allowed HDR color component value when using the HDR Color Picker (default value: 0).</param>
    /// <param name="maxBrightness">Maximum allowed HDR color component value when using the HDR Color Picker (default value: 8).</param>
    /// <param name="minExposureValue">Minimum exposure value allowed in the HDR Color Picker (default value: 1/8 = 0.125).</param>
    /// <param name="maxExposureValue">Maximum exposure value allowed in the HDR Color Picker (default value: 3).</param>
    [Obsolete("Brightness and exposure parameters are no longer used for anything. Use ColorUsageAttribute(bool showAlpha, bool hdr)")]
    public ColorUsageAttribute(
      bool showAlpha,
      bool hdr,
      float minBrightness,
      float maxBrightness,
      float minExposureValue,
      float maxExposureValue)
    {
      this.showAlpha = showAlpha;
      this.hdr = hdr;
      this.minBrightness = minBrightness;
      this.maxBrightness = maxBrightness;
      this.minExposureValue = minExposureValue;
      this.maxExposureValue = maxExposureValue;
    }
  }
}
