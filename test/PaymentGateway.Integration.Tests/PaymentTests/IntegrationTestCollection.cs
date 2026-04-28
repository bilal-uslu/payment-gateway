using PaymentGateway.Integration.Tests.Fixtures;

namespace PaymentGateway.Integration.Tests.PaymentTests;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<BankSimulatorFixture>
{
    public const string Name = "IntegrationTests";
}
