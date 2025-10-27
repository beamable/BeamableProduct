// this file was copied from nuget package Beamable.Common@6.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/6.1.0-PREVIEW.RC1

﻿using Beamable.Common.CronExpression;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
	public class MustBeValidCron : ValidationAttribute
	{
		private bool IsUsingCallbackMethod => !string.IsNullOrWhiteSpace(callbackMethodName);

		public readonly string callbackMethodName;
		public readonly BindingFlags bindingFlags;
		public MustBeValidCron() { }
		public MustBeValidCron(string callbackMethodNameNoArgumentsCallback, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
		{
			if (string.IsNullOrWhiteSpace(callbackMethodNameNoArgumentsCallback))
				throw new ArgumentException("Callback method name cannot be an empty string");

			callbackMethodName = callbackMethodNameNoArgumentsCallback;
			this.bindingFlags = bindingFlags;
		}

		public override void Validate(ContentValidationArgs args)
		{
			var validationField = args.ValidationField;
			var obj = args.Content;
			var ctx = args.Context;

			Schedule scheduleValue = null;
			bool fieldFound = true;
			switch (validationField.FieldType)
			{
				case { } t when t == typeof(OptionalSchedule):
					var optionalSchedule = validationField.GetValue<OptionalSchedule>();
					if (optionalSchedule.HasValue)
					{
						scheduleValue = optionalSchedule.Value;
					}
					break;

				case { } t when t == typeof(OptionalListingSchedule):
					var optionalListingSchedule = validationField.GetValue<OptionalListingSchedule>();
					if (optionalListingSchedule.HasValue)
					{
						scheduleValue = optionalListingSchedule.Value;
					}
					break;

				case { } t when t == typeof(OptionalEventSchedule):
					var optionalEventSchedule = validationField.GetValue<OptionalEventSchedule>();
					if (optionalEventSchedule.HasValue)
					{
						scheduleValue = optionalEventSchedule.Value;
					}
					break;
				
				case { } t when t == typeof(Schedule):
					scheduleValue = validationField.GetValue<Schedule>();
					break;

				case { } t when t == typeof(ListingSchedule):
					scheduleValue = validationField.GetValue<ListingSchedule>();
					break;

				case { } t when t == typeof(EventSchedule):
					scheduleValue = validationField.GetValue<EventSchedule>();
					break;
				default:
					fieldFound = false;
					break;
			}

			if (fieldFound)
			{
				if (scheduleValue != null)
				{
					ValidateCron(scheduleValue, validationField, obj, ctx);
				}
				
				if (IsUsingCallbackMethod)
				{
					validationField.Target.TryInvokeCallback(callbackMethodName, bindingFlags);
				}
				return;
			}

			throw new ContentValidationException(obj, validationField, "cron must be valid.");
		}

		public void ValidateCron(Schedule schedule, ValidationFieldWrapper validationField, IContentObject obj, IValidationContext ctx)
		{
			StringBuilder errorMessage = new StringBuilder();
			bool anyError = false;
			foreach (ScheduleDefinition scheduleDefinition in schedule.definitions)
			{
				string stringExpression = ExpressionParser.ScheduleDefinitionToCron(scheduleDefinition);	
				var parser = new ExpressionParser(stringExpression, new Options());
				try
				{
					parser.Parse(out ErrorData errorData);
					if (errorData.IsError)
					{
						anyError = true;
						errorMessage.AppendLine(errorData.ErrorMessage);
					}
				}
				catch (Exception e)
				{
					anyError = true;
					errorMessage.AppendLine(e.Message);
				}
			}
			
			if (anyError)
			{
				throw new ContentValidationException(obj, validationField, errorMessage.ToString());
			}
		}
	}
}
