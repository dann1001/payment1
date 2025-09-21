using GatewayService.AccountCharge.Domain.ValueObjects;




namespace GatewayService.AccountCharge.Domain.Invoices;
/// <summary>
/// Raw deposit observed from Nobitex GET /users/wallets/deposits/list.
/// This is an input to the domain (not persisted by itself).
/// </summary>
public sealed class IncomingDeposit
{
    public TransactionHash TxHash { get; }
    public ChainAddress Address { get; }
    public Money Amount { get; }
    public bool Confirmed { get; }
    public int Confirmations { get; }
    public int RequiredConfirmations { get; }
    public DateTimeOffset CreatedAt { get; }

    public IncomingDeposit(
        TransactionHash txHash,
        ChainAddress address,
        Money amount,
        bool confirmed,
        int confirmations,
        int requiredConfirmations,
        DateTimeOffset createdAt)
    {
        TxHash = txHash;
        Address = address;
        Amount = amount;
        Confirmed = confirmed;
        Confirmations = confirmations;
        RequiredConfirmations = requiredConfirmations;
        CreatedAt = createdAt;
    }
}
