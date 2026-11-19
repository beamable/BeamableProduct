using System;
using System.Collections.Generic;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Player;
using Beamable.Stats;
using NUnit.Framework;

namespace Beamable.Platform.Tests.Auth.AuthServiceTests
{
	public class PasswordResetConfirmationTests : AuthServiceTestBase
	{
		private const string ConfirmRoute = ROUTE + "/password-update/confirm";
		private const string Code = "014928";
		private const string NewPassword = "new-password";
		private const string FirstEmail = "first@example.test";
		private const string SecondEmail = "second@example.test";

		private class OfflineAccounts : PlayerAccounts
		{
			public OfflineAccounts(IAuthService auth, IBeamableRequester requester, IDependencyProvider provider)
				: base(null, auth, null, requester, null, null, provider) { }

			protected override Promise PerformRefresh() => Promise.Success;
		}

		private PlayerAccounts CreateAccounts()
		{
			var dependencies = new DependencyBuilder();
			dependencies.AddSingleton<IAuthService>(_service);
			var accounts = new OfflineAccounts(_service, _requester, dependencies.Build());
			var user = new User { id = 1, email = FirstEmail, thirdPartyAppAssociations = new List<string>() };
			accounts.Current = new PlayerAccount(accounts, null, user, new UserExtensions.StatCollection());
			return accounts;
		}

		[Test]
		public void LegacyConfirmationOmitsEmailAfterTwoResetRequests()
		{
			var accounts = CreateAccounts();
			var init = _requester.MockRequest<EmptyResponse>(Method.POST, ROUTE + "/password-update/init")
				.WithResponse(new EmptyResponse());
			var confirm = _requester.MockRequest<EmptyResponse>(Method.POST, ConfirmRoute)
				.WithoutJsonField("email")
				.WithJsonFieldMatch("code", Code)
				.WithJsonFieldMatch("newPassword", NewPassword)
				.WithResponse(new EmptyResponse());

			accounts.ResetPassword(FirstEmail).GetResult();
			accounts.ResetPassword(SecondEmail).GetResult();
#pragma warning disable CS0618
			var result = accounts.ConfirmPassword(Code, NewPassword).GetResult();
#pragma warning restore CS0618

			Assert.That(result.isSuccess, Is.True);
			Assert.That(init.CallCount, Is.EqualTo(2));
			Assert.That(confirm.CallCount, Is.EqualTo(1));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void ExplicitConfirmationReturnsFailureWithoutSendingRequest(string email)
		{
			var accounts = CreateAccounts();
			var confirm = _requester.MockRequest<EmptyResponse>(Method.POST, ConfirmRoute)
				.WithResponse(new EmptyResponse());

			var result = accounts.ConfirmPassword(email, Code, NewPassword).GetResult();

			Assert.That(result.isSuccess, Is.False);
			Assert.That(result.innerException, Is.InstanceOf<ArgumentException>());
			Assert.That(confirm.CallCount, Is.Zero);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void DirectAuthConfirmationRejectsBlankEmailWithoutSendingRequest(string email)
		{
			var confirm = _requester.MockRequest<EmptyResponse>(Method.POST, ConfirmRoute)
				.WithResponse(new EmptyResponse());

			var result = _service.ConfirmPasswordUpdate(email, Code, NewPassword);
			Assert.Throws<ArgumentException>(() => result.GetAwaiter().GetResult());
			Assert.That(confirm.CallCount, Is.Zero);
		}
	}
}
