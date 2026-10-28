using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Common.Shop;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class MustBeComparatorStringTests
	{
		// Each set mirrors the backend evaluator for that consumer:
		// stat -> PlayerStatRequirement.meetsRequirement, offer -> PlayerOfferStatusModel, cohort -> CommerceModel.meetsCohortRequirements.
		[TestCase("eq"), TestCase("equal")]
		[TestCase("ne"), TestCase("neq"), TestCase("notequal")]
		[TestCase("gt"), TestCase("greaterthan")]
		[TestCase("ge"), TestCase("gte"), TestCase("greaterthanequal")]
		[TestCase("lt"), TestCase("lessthan")]
		[TestCase("le"), TestCase("lte"), TestCase("lessthanequal")]
		public void Stat_AcceptsBackendValues(string value)
		{
			Assert.DoesNotThrow(() => Validate(stat: value));
		}

		[TestCase("in"), TestCase("nin"), TestCase("notin"), TestCase("EQ"), TestCase("")]
		public void Stat_RejectsUnsupportedValues(string value)
		{
			Assert.Throws<AggregateContentValidationException>(() => Validate(stat: value));
		}

		[TestCase("eq"), TestCase("ne"), TestCase("gt"), TestCase("ge"), TestCase("lt"), TestCase("le")]
		public void OfferPurchases_AcceptsBackendValues(string value)
		{
			Assert.DoesNotThrow(() => Validate(offer: value));
		}

		[TestCase("gte"), TestCase("lte"), TestCase("neq"), TestCase("equal"), TestCase("greaterthan"), TestCase("in")]
		public void OfferPurchases_RejectsUnsupportedValues(string value)
		{
			Assert.Throws<AggregateContentValidationException>(() => Validate(offer: value));
		}

		[TestCase("eq"), TestCase("ne")]
		public void CohortMembership_AcceptsBackendValues(string value)
		{
			Assert.DoesNotThrow(() => Validate(cohort: value));
		}

		[TestCase("in"), TestCase("nin"), TestCase("equal"), TestCase("neq"), TestCase("gt")]
		public void CohortMembership_RejectsUnsupportedValues(string value)
		{
			Assert.Throws<AggregateContentValidationException>(() => Validate(cohort: value));
		}

		[TestCase(MustBeComparatorString.EQUALS)]
		[TestCase(MustBeComparatorString.NOT_EQUALS)]
		[TestCase(MustBeComparatorString.GREATER_THAN)]
		[TestCase(MustBeComparatorString.GREATER_THAN_OR_EQUAL)]
		[TestCase(MustBeComparatorString.LESS_THAN)]
		[TestCase(MustBeComparatorString.LESS_THAN_OR_EQUAL)]
		public void DefaultKind_AcceptsLegacyValues(string value)
		{
			var attribute = new MustBeComparatorString();
			Assert.AreEqual(ComparatorStringKind.STAT, attribute.Kind);
			Assert.IsTrue(attribute.PossibleValues.Contains(value));
		}

		[TestCase(ComparatorStringKind.STAT, "le", "lte")]
		[TestCase(ComparatorStringKind.STAT, "lessthanequal", "lte")]
		[TestCase(ComparatorStringKind.STAT, "ne", "neq")]
		[TestCase(ComparatorStringKind.OFFER_PURCHASES, "le", "le")]
		[TestCase(ComparatorStringKind.COHORT_MEMBERSHIP, "ne", "ne")]
		public void TryGetOption_MapsAliasToCanonicalValue(ComparatorStringKind kind, string value, string expected)
		{
			var attribute = new MustBeComparatorString(kind);
			Assert.IsTrue(attribute.TryGetOption(value, out var option));
			Assert.AreEqual(expected, option.Value);
		}

		[TestCase(ComparatorStringKind.OFFER_PURCHASES, "lte")]
		[TestCase(ComparatorStringKind.COHORT_MEMBERSHIP, "neq")]
		public void TryGetOption_UnknownForKind_ReturnsFalse(ComparatorStringKind kind, string value)
		{
			var attribute = new MustBeComparatorString(kind);
			Assert.IsFalse(attribute.TryGetOption(value, out _));
		}

		[TestCase(typeof(StatRequirement), ComparatorStringKind.STAT)]
		[TestCase(typeof(OfferConstraint), ComparatorStringKind.OFFER_PURCHASES)]
		[TestCase(typeof(CohortRequirement), ComparatorStringKind.COHORT_MEMBERSHIP)]
		public void ListingRequirements_UseMatchingKind(System.Type type, ComparatorStringKind expected)
		{
			var attribute = type.GetField("constraint").GetCustomAttribute<MustBeComparatorString>();
			Assert.IsNotNull(attribute);
			Assert.AreEqual(expected, attribute.Kind);
		}

		private static void Validate(string stat = "eq", string offer = "eq", string cohort = "eq")
		{
			var content = ScriptableObject.CreateInstance<ComparatorTestContent>();
			content.stat = stat;
			content.offer = offer;
			content.cohort = cohort;
			content.Validate(new ValidationContext());
		}
	}

	public class ComparatorTestContent : ContentObject
	{
		[MustBeComparatorString]
		public string stat;

		[MustBeComparatorString(ComparatorStringKind.OFFER_PURCHASES)]
		public string offer;

		[MustBeComparatorString(ComparatorStringKind.COHORT_MEMBERSHIP)]
		public string cohort;
	}
}
