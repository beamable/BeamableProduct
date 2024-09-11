using System;
using System.Globalization;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
  /// <summary>
  ///   <para>A 2D Rectangle defined by X and Y position, width and height.</para>
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public struct Rect : IEquatable<Rect>
  {
    private float _xMin;
    private float _yMin;
    private float _Width;
    private float _height;

    /// <summary>
    ///   <para>Creates a new rectangle.</para>
    /// </summary>
    /// <param name="x">The X value the rect is measured from.</param>
    /// <param name="y">The Y value the rect is measured from.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rect(float x, float y, float width, float height)
    {
      _xMin = x;
      _yMin = y;
      _Width = width;
      _height = height;
    }

    /// <summary>
    ///   <para>Creates a rectangle given a size and position.</para>
    /// </summary>
    /// <param name="position">The position of the minimum corner of the rect.</param>
    /// <param name="size">The width and height of the rect.</param>
    public Rect(Vector2 position, Vector2 size)
    {
      _xMin = position.x;
      _yMin = position.y;
      _Width = size.x;
      _height = size.y;
    }

    /// <summary>
    ///   <para></para>
    /// </summary>
    /// <param name="source"></param>
    public Rect(Rect source)
    {
      _xMin = source._xMin;
      _yMin = source._yMin;
      _Width = source._Width;
      _height = source._height;
    }

    /// <summary>
    ///   <para>Shorthand for writing new Rect(0,0,0,0).</para>
    /// </summary>
    public static Rect zero { get; } = new Rect(0.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    ///   <para>Creates a rectangle from min/max coordinate values.</para>
    /// </summary>
    /// <param name="xmin">The minimum X coordinate.</param>
    /// <param name="ymin">The minimum Y coordinate.</param>
    /// <param name="xmax">The maximum X coordinate.</param>
    /// <param name="ymax">The maximum Y coordinate.</param>
    /// <returns>
    ///   <para>A rectangle matching the specified coordinates.</para>
    /// </returns>
    public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax)
    {
      return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
    }

    /// <summary>
    ///   <para>Set components of an existing Rect.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Set(float x, float y, float width, float height)
    {
      _xMin = x;
      _yMin = y;
      _Width = width;
      _height = height;
    }

    /// <summary>
    ///   <para>The X coordinate of the rectangle.</para>
    /// </summary>
    public float x
    {
      get
      {
        return _xMin;
      }
      set
      {
        _xMin = value;
      }
    }

    /// <summary>
    ///   <para>The Y coordinate of the rectangle.</para>
    /// </summary>
    public float y
    {
      get
      {
        return _yMin;
      }
      set
      {
        _yMin = value;
      }
    }

    /// <summary>
    ///   <para>The X and Y position of the rectangle.</para>
    /// </summary>
    public Vector2 position
    {
      get
      {
        return new Vector2(_xMin, _yMin);
      }
      set
      {
        _xMin = value.x;
        _yMin = value.y;
      }
    }

    /// <summary>
    ///   <para>The position of the center of the rectangle.</para>
    /// </summary>
    public Vector2 center
    {
      get
      {
        return new Vector2(x + _Width / 2f, y + _height / 2f);
      }
      set
      {
        _xMin = value.x - _Width / 2f;
        _yMin = value.y - _height / 2f;
      }
    }

    /// <summary>
    ///   <para>The position of the minimum corner of the rectangle.</para>
    /// </summary>
    public Vector2 min
    {
      get
      {
        return new Vector2(xMin, yMin);
      }
      set
      {
        xMin = value.x;
        yMin = value.y;
      }
    }

    /// <summary>
    ///   <para>The position of the maximum corner of the rectangle.</para>
    /// </summary>
    public Vector2 max
    {
      get
      {
        return new Vector2(xMax, yMax);
      }
      set
      {
        xMax = value.x;
        yMax = value.y;
      }
    }

    /// <summary>
    ///   <para>The width of the rectangle, measured from the X position.</para>
    /// </summary>
    public float width
    {
      get
      {
        return _Width;
      }
      set
      {
        _Width = value;
      }
    }

    /// <summary>
    ///   <para>The height of the rectangle, measured from the Y position.</para>
    /// </summary>
    public float height
    {
      get
      {
        return _height;
      }
      set
      {
        _height = value;
      }
    }

    /// <summary>
    ///   <para>The width and height of the rectangle.</para>
    /// </summary>
    public Vector2 size
    {
      get
      {
        return new Vector2(_Width, _height);
      }
      set
      {
        _Width = value.x;
        _height = value.y;
      }
    }

    /// <summary>
    ///   <para>The minimum X coordinate of the rectangle.</para>
    /// </summary>
    public float xMin
    {
      get
      {
        return _xMin;
      }
      set
      {
        float xMax = this.xMax;
        _xMin = value;
        _Width = xMax - _xMin;
      }
    }

    /// <summary>
    ///   <para>The minimum Y coordinate of the rectangle.</para>
    /// </summary>
    public float yMin
    {
      get
      {
        return _yMin;
      }
      set
      {
        float yMax = this.yMax;
        _yMin = value;
        _height = yMax - _yMin;
      }
    }

    /// <summary>
    ///   <para>The maximum X coordinate of the rectangle.</para>
    /// </summary>
    public float xMax
    {
      get
      {
        return _Width + _xMin;
      }
      set
      {
        _Width = value - _xMin;
      }
    }

    /// <summary>
    ///   <para>The maximum Y coordinate of the rectangle.</para>
    /// </summary>
    public float yMax
    {
      get
      {
        return _height + _yMin;
      }
      set
      {
        _height = value - _yMin;
      }
    }

    /// <summary>
    ///   <para>Returns true if the x and y components of point is a point inside this rectangle. If allowInverse is present and true, the width and height of the Rect are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>
    ///   <para>True if the point lies within the specified rectangle.</para>
    /// </returns>
    public bool Contains(Vector2 point)
    {
      return point.x >= (double) xMin && point.x < (double) xMax && point.y >= (double) yMin && point.y < (double) yMax;
    }

    /// <summary>
    ///   <para>Returns true if the x and y components of point is a point inside this rectangle. If allowInverse is present and true, the width and height of the Rect are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>
    ///   <para>True if the point lies within the specified rectangle.</para>
    /// </returns>
    public bool Contains(Vector3 point)
    {
      return point.x >= (double) xMin && point.x < (double) xMax && point.y >= (double) yMin && point.y < (double) yMax;
    }

    /// <summary>
    ///   <para>Returns true if the x and y components of point is a point inside this rectangle. If allowInverse is present and true, the width and height of the Rect are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <param name="allowInverse">Does the test allow the Rect's width and height to be negative?</param>
    /// <returns>
    ///   <para>True if the point lies within the specified rectangle.</para>
    /// </returns>
    public bool Contains(Vector3 point, bool allowInverse)
    {
      if (!allowInverse)
        return Contains(point);
      bool flag = false;
      if (width < 0.0 && point.x <= (double) xMin && point.x > (double) xMax || width >= 0.0 && point.x >= (double) xMin && point.x < (double) xMax)
        flag = true;
      return flag && (height < 0.0 && point.y <= (double) yMin && point.y > (double) yMax || height >= 0.0 && point.y >= (double) yMin && point.y < (double) yMax);
    }

    private static Rect OrderMinMax(Rect rect)
    {
      if (rect.xMin > (double) rect.xMax)
      {
        float xMin = rect.xMin;
        rect.xMin = rect.xMax;
        rect.xMax = xMin;
      }
      if (rect.yMin > (double) rect.yMax)
      {
        float yMin = rect.yMin;
        rect.yMin = rect.yMax;
        rect.yMax = yMin;
      }
      return rect;
    }

    /// <summary>
    ///   <para>Returns true if the other rectangle overlaps this one. If allowInverse is present and true, the widths and heights of the Rects are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="other">Other rectangle to test overlapping with.</param>
    public bool Overlaps(Rect other)
    {
      return other.xMax > (double) xMin && other.xMin < (double) xMax && other.yMax > (double) yMin && other.yMin < (double) yMax;
    }

    /// <summary>
    ///   <para>Returns true if the other rectangle overlaps this one. If allowInverse is present and true, the widths and heights of the Rects are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="other">Other rectangle to test overlapping with.</param>
    /// <param name="allowInverse">Does the test allow the widths and heights of the Rects to be negative?</param>
    public bool Overlaps(Rect other, bool allowInverse)
    {
      Rect rect = this;
      if (allowInverse)
      {
        rect = OrderMinMax(rect);
        other = OrderMinMax(other);
      }
      return rect.Overlaps(other);
    }

    /// <summary>
    ///   <para>Returns a point inside a rectangle, given normalized coordinates.</para>
    /// </summary>
    /// <param name="rectangle">Rectangle to get a point inside.</param>
    /// <param name="normalizedRectCoordinates">Normalized coordinates to get a point for.</param>
    public static Vector2 NormalizedToPoint(
      Rect rectangle,
      Vector2 normalizedRectCoordinates)
    {
      return new Vector2(Mathf.Lerp(rectangle.x, rectangle.xMax, normalizedRectCoordinates.x), Mathf.Lerp(rectangle.y, rectangle.yMax, normalizedRectCoordinates.y));
    }

    /// <summary>
    ///   <para>Returns the normalized coordinates cooresponding the the point.</para>
    /// </summary>
    /// <param name="rectangle">Rectangle to get normalized coordinates inside.</param>
    /// <param name="point">A point inside the rectangle to get normalized coordinates for.</param>
    public static Vector2 PointToNormalized(Rect rectangle, Vector2 point)
    {
      return new Vector2(Mathf.InverseLerp(rectangle.x, rectangle.xMax, point.x), Mathf.InverseLerp(rectangle.y, rectangle.yMax, point.y));
    }

    public static bool operator !=(Rect lhs, Rect rhs)
    {
      return !(lhs == rhs);
    }

    public static bool operator ==(Rect lhs, Rect rhs)
    {
      return lhs.x == (double) rhs.x && lhs.y == (double) rhs.y && lhs.width == (double) rhs.width && lhs.height == (double) rhs.height;
    }

    public override int GetHashCode()
    {
      float num1 = x;
      int hashCode = num1.GetHashCode();
      num1 = width;
      int num2 = num1.GetHashCode() << 2;
      int num3 = hashCode ^ num2;
      num1 = y;
      int num4 = num1.GetHashCode() >> 2;
      int num5 = num3 ^ num4;
      num1 = height;
      int num6 = num1.GetHashCode() >> 1;
      return num5 ^ num6;
    }

    public override bool Equals(object other)
    {
      return other is Rect other1 && Equals(other1);
    }

    public bool Equals(Rect other)
    {
      int num1;
      if (x.Equals(other.x))
      {
        float num2 = y;
        if (num2.Equals(other.y))
        {
          num2 = width;
          if (num2.Equals(other.width))
          {
            num2 = height;
            num1 = num2.Equals(other.height) ? 1 : 0;
            goto label_5;
          }
        }
      }
      num1 = 0;
label_5:
      return num1 != 0;
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this Rect.</para>
    /// </summary>
    public override string ToString()
    {
      return $"(x:{(object) x:F2}, y:{(object) y:F2}, width:{(object) width:F2}, height:{(object) height:F2})";
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this Rect.</para>
    /// </summary>
    /// <param name="format"></param>
    public string ToString(string format)
    {
      return
        $"(x:{(object) x.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, y:{(object) y.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, width:{(object) width.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, height:{(object) height.ToString(format, CultureInfo.InvariantCulture.NumberFormat)})";
    }

    [Obsolete("use xMin")]
    public float left
    {
      get
      {
        return _xMin;
      }
    }

    [Obsolete("use xMax")]
    public float right
    {
      get
      {
        return _xMin + _Width;
      }
    }

    [Obsolete("use yMin")]
    public float top
    {
      get
      {
        return _yMin;
      }
    }

    [Obsolete("use yMax")]
    public float bottom
    {
      get
      {
        return _yMin + _height;
      }
    }
  }
}
