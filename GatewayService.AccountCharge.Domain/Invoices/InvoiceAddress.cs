public sealed class InvoiceAddress
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }   // ← اضافه کن (FK صریح)

    public int WalletId { get; private set; }
    public string Currency { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string? Network { get; private set; }
    public string? Tag { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }  // ← Offset

    private InvoiceAddress() { }

    public InvoiceAddress(int walletId, string currency, string address, string? network, string? tag, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        WalletId = walletId;
        Currency = currency;
        Address = address;
        Network = network;
        Tag = tag;
        CreatedAt = createdAt;
    }
}
