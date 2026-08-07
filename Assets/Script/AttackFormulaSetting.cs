using System;
using UnityEngine;

public enum AttackFormulaType
{
    Formula1,
    Formula2,
    Formula3,
    Formula4,
    Formula5,
    Formula6,
    Formula7
}

[Serializable]
public class AttackFormulaSetting
{
    //public string displayName;
    public AttackFormulaType formulaType;
}