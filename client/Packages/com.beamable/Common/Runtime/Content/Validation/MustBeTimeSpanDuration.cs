using System;
using System.Globalization;

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
    public class MustBeTimeSpanDuration : ValidationAttribute
    {
        public override void Validate(ContentValidationArgs args)
        {
            var validationField = args.ValidationField;
            var obj = args.Content;
            var ctx = args.Context;

            if (validationField.FieldType == typeof(OptionalString))
            {
                var optional = validationField.GetValue<OptionalString>();
                if (optional.HasValue)
                {
                    ValidateString(optional.Value, validationField, obj, ctx);
                }

                return;
            }

            if (validationField.FieldType == typeof(string))
            {
                var strValue = validationField.GetValue<string>();
                ValidateString(strValue, validationField, obj, ctx);
                return;
            }

            throw new ContentValidationException(obj, validationField, "duration must be a string field.");
        }

        public void ValidateString(string strValue, ValidationFieldWrapper validationField, IContentObject obj, IValidationContext ctx)
        {
            if (string.IsNullOrEmpty(strValue))
            {
                throw new ContentValidationException(obj, validationField, "duration cannot be an empty string.");
            }

            if (!TimeSpan.TryParseExact(strValue, @"\P%d\D", CultureInfo.InvariantCulture, TimeSpanStyles.None, out _))
            {
                throw new ContentValidationException(obj, validationField, "duration must be expressed in days. e.g. P30D");
            }
        }
    }
}