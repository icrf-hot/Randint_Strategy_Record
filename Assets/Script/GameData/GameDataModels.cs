using System;

namespace Randint.Data
{
    // JSON은 배열로 저장하고, 로딩 후 ID 조회용 Dictionary로 변환합니다.
    [Serializable] public sealed class DataManifest
    {
        public int schemaVersion;
        public string locale;
        public DataFile[] files;
        public string[] requiredTextKeys;
    }

    [Serializable] public sealed class DataFile
    {
        public string kind;
        public string path;
    }

    [Serializable] public sealed class DataTable<T>
    {
        public int schemaVersion;
        public T[] entries;
    }

    [Serializable] public sealed class TextEntry
    {
        public string key;
        public string value;
        // 미작성 대사처럼 의도적으로 빈 문구만 허용합니다. 누락 키와는 다릅니다.
        public bool allowEmpty;
    }

    [Serializable] public class StatDefinition
    {
        public string id;
        public int maxHP = -1;
        public int attack = -1;
        public int defense = -1;
        public int artsResistance = -1;
        public float attackSpeed = -1f;
    }

    [Serializable] public sealed class OperatorDefinition : StatDefinition
    {
        public string nameKey;
        public string infoKey;
        public string position;
        public string classId;
        public bool requiresEnemyTarget;
        public string[] skillIds;
    }

    [Serializable] public sealed class EnemyDefinition : StatDefinition { }

    [Serializable] public sealed class SkillDefinition
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public int maxSP = -1;
        public string chargeType;
        public int chargeAmount = -1;
    }
}
