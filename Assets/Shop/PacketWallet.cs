public sealed class PacketWallet
{
    public int Balance { get; private set; }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && amount <= Balance;
    }

    public bool TryAdd(int amount)
    {
        if (amount < 0 || amount > int.MaxValue - Balance)
            return false;

        Balance += amount;
        return true;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;

        Balance -= amount;
        return true;
    }

    public void Clear()
    {
        Balance = 0;
    }
}
