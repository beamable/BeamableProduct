using NUnit.Framework;
using cli.Services.Web.Helpers;

namespace tests.Web.Helpers;

[TestFixture]
public class OpenApiMethodNameGeneratorTests
{
	[TestCase("/api/customers/{customerId}/realms/{realmId}/secrets/values", "GET", "customersGetRealmsSecretsValues")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}", "GET", "customersGetRealmsSecretsValuesByKey")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}", "PUT", "customersPutRealmsSecretsValuesByKey")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/secrets/values/{key}", "DELETE", "customersDeleteRealmsSecretsValuesByKey")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/support/tickets", "GET", "customersGetRealmsSupportTickets")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}", "GET", "customersGetRealmsSupportTicketsByTicketId")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}", "PATCH", "customersPatchRealmsSupportTicketsByTicketId")]
	[TestCase("/api/customers/{customerId}/realms/{realmId}/support/tickets/{ticketId}", "DELETE", "customersDeleteRealmsSupportTicketsByTicketId")]
	public void GenerateMethodName_DisambiguatesTrailingPathParameters(string endpoint, string httpMethod, string expected)
	{
		var actual = OpenApiMethodNameGenerator.GenerateMethodName(endpoint, httpMethod);
		Assert.AreEqual(expected, actual);
	}

	[TestCase("/basic/inventory/preview", "POST", "inventoryPostPreviewBasic")]
	[TestCase("/api/internal/users/{userId}/avatar", "GET", "usersGetAvatarByUserIdInternal")]
	[TestCase("/api/internal/users/{userId}", "GET", "usersGetByUserIdInternal")]
	public void GenerateMethodName_PreservesExistingNamingForSinglePathParam(string endpoint, string httpMethod, string expected)
	{
		var actual = OpenApiMethodNameGenerator.GenerateMethodName(endpoint, httpMethod);
		Assert.AreEqual(expected, actual);
	}
}
