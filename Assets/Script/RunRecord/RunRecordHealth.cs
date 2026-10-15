using System;

public sealed class RunRecordHealth
{
    public const int Maximum = 100;
    public int Integrity { get; private set; } = Maximum;
    public int Consistency { get; private set; } = Maximum;

    public bool ReduceIntegrity(int amount)
    {
        if (amount < 0) return false;
        Integrity = Math.Max(0, Integrity - amount);
        return true;
    }

    public bool ReduceConsistency(int amount)
    {
        if (amount < 0) return false;
        int consumed = Math.Min(Consistency, amount);
        Consistency -= consumed;
        return ReduceIntegrity(amount - consumed);
    }

    public void Restore(int integrity, int consistency)
    {
        if (integrity < 0 || integrity > Maximum || consistency < 0 || consistency > Maximum)
            throw new ArgumentOutOfRangeException(nameof(integrity));
        Integrity = integrity;
        Consistency = consistency;
    }

    public void Reset() => Restore(Maximum, Maximum);
}
