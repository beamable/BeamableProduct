using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class BsonElementAttribute : Attribute, IBsonMemberMapAttribute
	{
		private string _elementName;
		private int _order = int.MaxValue;

		public BsonElementAttribute()
		{
		}

		public BsonElementAttribute(string elementName) => this._elementName = elementName;

		public string ElementName => this._elementName;

		public int Order
		{
			get => this._order;
			set => this._order = value;
		}

		/// <summary>
		/// Do not use this method from Unity.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		[Obsolete("this method is not meant to be used from Unity, and will throw an exception")]
		public void Apply()
		{
			throw new NotImplementedException(nameof(Apply) + " is not supported in Unity");
		}
	}
}
#endif
