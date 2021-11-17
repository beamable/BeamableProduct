using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Inventory;
using UnityEngine;

namespace Beamable.Common.Content.Validation
{
	/// <summary>
	/// This type defines part of the %Beamable %ContentObject validation process.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.Validation.ValidationAttribute script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	public class MustReferenceContent : ValidationAttribute
	{
		public bool AllowNull
		{
			get;
			set;
		}

		public Type[] AllowedTypes
		{
			get;
			set;
		}

		public MustReferenceContent(bool allowNull = false, params Type[] allowedTypes)
		{
			AllowNull = allowNull;
			AllowedTypes = allowedTypes;
		}

		public override void Validate(ContentValidationArgs args)
		{
			var field = args.ValidationField;
			var obj = args.Content;
			var ctx = args.Context;

			// this works for ContentRefs, or strings...
			if (typeof(string) == field.FieldType)
			{
				ValidateAsString(field, obj, ctx);
				return;
			}

			var isListOfObject = typeof(IList).IsAssignableFrom(field.FieldType);
			if (isListOfObject)
			{
				if (args.IsArray)
				{
					ValidateAsObjectList(field, obj, ctx, args.ArrayIndex);
					return;
				}
				else
				{
					var refs = (field.GetValue() as IList);
					if (refs != null)
					{
						for (var i = 0; i < refs.Count; i++)
						{
							ValidateAsObjectList(field, obj, ctx, i);
						}
					}

					return;
				}
			}

			if (typeof(IContentRef).IsAssignableFrom(field.FieldType))
			{
				ValidateAsReference(field, obj, ctx);
				return;
			}

			throw new ContentValidationException(
				obj, field, "MustReferenceContent only works for IContentRef or String fields");
		}

		void ValidateAsReference(ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx)
		{
			var reference = field.GetValue() as IContentRef;

			if (reference == null)
			{
				throw new ContentValidationException(obj, field, "reference cannot be null");
			}

			var id = reference.GetId();
			ValidateId(id, field, obj, ctx);
		}

		void ValidateAsString(ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx)
		{
			var id = field.GetValue() as string;
			ValidateId(id, field, obj, ctx);
		}

		void ValidateAsStringList(ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx, int index)
		{
			var ids = field.GetValue() as IEnumerable<string>;
			ValidateId(ids.ToArray()[index], field, obj, ctx);
		}

		void ValidateAsRefList(ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx, int index)
		{
			var refs = field.GetValue() as IEnumerable<ContentRef>;
			ValidateId(refs.ToArray()[index]?.GetId(), field, obj, ctx);
		}

		void ValidateAsObjectList(ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx, int index)
		{
			var list = field.GetValue() as IList;
			var elem = list[index];

			switch (elem)
			{
				case string id:
					ValidateId(id, field, obj, ctx);
					break;
				case IContentRef reference:
					ValidateId(reference.GetId(), field, obj, ctx);
					break;
			}
		}

		void ValidateId(string id, ValidationFieldWrapper field, IContentObject obj, IValidationContext ctx)
		{
			// check for null strings
			if (string.IsNullOrEmpty(id))
			{
				if (AllowNull) return;
				throw new ContentValidationException(obj, field, "reference cannot be null. ");
			}

			// check for valid types on the id string
			if (AllowedTypes.Length > 0)
			{
				// the content must be one of the types...
				var typeNames = AllowedTypes.Select(type => ctx.GetTypeName(type)).ToList();

				if (!typeNames.Any(id.StartsWith))
				{
					var preMessage = typeNames.Count == 1 ? "reference must be a" : "reference must be one of";
					throw new ContentValidationException(obj, field, $"{preMessage} [{string.Join(",", typeNames)}]");
				}
			}

			// finally, check that the validated type id actually exists within the context
			if (!ctx.ContentExists(id))
			{
				throw new ContentValidationException(obj, field, "reference does not exist. ");
			}
		}
	}
}
