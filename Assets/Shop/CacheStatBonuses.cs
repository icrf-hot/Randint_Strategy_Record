using System;
using System.Collections.Generic;

public readonly struct CacheStatBonuses
{
    public int MaxHP { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int ArtsResistance { get; }
    public float AttackSpeed { get; }

    public CacheStatBonuses(IReadOnlyList<CacheData> items)
    {
        long hp = 0, attack = 0, defense = 0, resistance = 0;
        double speed = 0;
        foreach (CacheData item in items)
        {
            if (item == null || !item.IsConfigured || item.EffectKind != CacheEffectKind.GlobalOperatorStats)
                continue;
            hp += item.MaxHPBonus;
            attack += item.AttackBonus;
            defense += item.DefenseBonus;
            resistance += item.ArtsResistanceBonus;
            speed += item.AttackSpeedBonus;
        }
        MaxHP = (int)Math.Min(int.MaxValue, hp);
        Attack = (int)Math.Min(int.MaxValue, attack);
        Defense = (int)Math.Min(int.MaxValue, defense);
        ArtsResistance = (int)Math.Min(int.MaxValue, resistance);
        AttackSpeed = (float)Math.Min(float.MaxValue, speed);
    }

    public static int Add(int value, int bonus, int roundBonus = 0)
    {
        return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, (long)value + bonus + roundBonus));
    }
}
