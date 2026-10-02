using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Randint.Data
{
    public sealed class GameDataValidationException : Exception
    {
        public GameDataValidationException(IEnumerable<string> errors)
            : base("[GameData] 검증 실패:\n" + string.Join("\n", errors)) { }
    }

    public sealed class GameDataCatalog
    {
        private readonly Dictionary<string, TextEntry> texts = new Dictionary<string, TextEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, OperatorDefinition> operators = new Dictionary<string, OperatorDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, EnemyDefinition> enemies = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillDefinition> skills = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        private static readonly Regex IdPattern = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)*$");
        public string Locale { get; private set; }

        // Resources 외의 공급자도 받을 수 있어 테스트에서 실제 파일을 바꾸지 않고 검증합니다.
        public static GameDataCatalog Load(Func<string, string> read, string manifestPath = "GameData/manifest")
        {
            var errors = new List<string>();
            var catalog = new GameDataCatalog();
            DataManifest manifest = Parse<DataManifest>(read, manifestPath, errors);
            if (manifest == null) throw new GameDataValidationException(errors);
            if (manifest.schemaVersion != 1) errors.Add($"{manifestPath}: schemaVersion은 1이어야 합니다.");
            if (string.IsNullOrWhiteSpace(manifest.locale)) errors.Add($"{manifestPath}: locale 누락");
            catalog.Locale = manifest.locale;
            var paths = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<string>(StringComparer.Ordinal);
            if (manifest.files == null || manifest.files.Length == 0) errors.Add($"{manifestPath}: files 누락");
            foreach (DataFile file in manifest.files ?? Array.Empty<DataFile>())
            {
                if (file == null || string.IsNullOrWhiteSpace(file.path) ||
                    !file.path.StartsWith("GameData/", StringComparison.Ordinal) ||
                    file.path.Contains("..") || file.path.Contains("\\") || file.path.EndsWith(".json", StringComparison.Ordinal))
                {
                    errors.Add($"{manifestPath}: 유효하지 않은 Resources 경로 '{file?.path}'");
                    continue;
                }
                if (!paths.Add(file.path)) { errors.Add($"{manifestPath}: 중복 파일 '{file.path}'"); continue; }
                kinds.Add(file.kind ?? "");
                switch (file.kind)
                {
                    case "text": catalog.AddTable(read, file.path, catalog.texts, x => x.key, errors); break;
                    case "operator": catalog.AddTable(read, file.path, catalog.operators, x => x.id, errors); break;
                    case "enemy": catalog.AddTable(read, file.path, catalog.enemies, x => x.id, errors); break;
                    case "skill": catalog.AddTable(read, file.path, catalog.skills, x => x.id, errors); break;
                    default: errors.Add($"{file.path}: 알 수 없는 kind '{file.kind}'"); break;
                }
            }
            foreach (string kind in new[] { "text", "operator", "enemy", "skill" })
                if (!kinds.Contains(kind)) errors.Add($"{manifestPath}: '{kind}' 테이블 누락");

            // 모든 테이블을 등록한 뒤 참조를 검사하므로 파일 순서에는 의존하지 않습니다.
            foreach (TextEntry entry in catalog.texts.Values)
                if (entry.value == null || (!entry.allowEmpty && string.IsNullOrWhiteSpace(entry.value)))
                    errors.Add($"text '{entry.key}': value 누락 또는 미허용 공백");
            foreach (OperatorDefinition op in catalog.operators.Values)
            {
                ValidateStats(op, errors);
                catalog.CheckText(op.nameKey, op.id, errors);
                catalog.CheckText(op.infoKey, op.id, errors);
                string allowed = op.position == "Front" ? "Defender|Guard|Vanguard" :
                    op.position == "Middle" ? "Caster|Sniper" : op.position == "Back" ? "AttackHealer|Healer|Supporter" : "";
                if (allowed.Length == 0 || Array.IndexOf(allowed.Split('|'), op.classId) < 0)
                    errors.Add($"operator '{op.id}': position/classId 조합 오류 ({op.position}/{op.classId})");
                if (op.skillIds == null) errors.Add($"operator '{op.id}': skillIds 누락 (없으면 [] 사용)");
                var seenSkills = new HashSet<string>(StringComparer.Ordinal);
                foreach (string id in op.skillIds ?? Array.Empty<string>())
                {
                    if (id == null || !catalog.skills.ContainsKey(id)) errors.Add($"operator '{op.id}': 누락 skill 참조 '{id}'");
                    if (!seenSkills.Add(id ?? "")) errors.Add($"operator '{op.id}': 중복 skill 참조 '{id}'");
                }
            }
            foreach (EnemyDefinition enemy in catalog.enemies.Values) ValidateStats(enemy, errors);
            foreach (SkillDefinition skill in catalog.skills.Values)
            {
                catalog.CheckText(skill.nameKey, skill.id, errors);
                catalog.CheckText(skill.descriptionKey, skill.id, errors);
                if (skill.maxSP <= 0 || skill.chargeAmount < 0 ||
                    Array.IndexOf(new[] { "Natural", "Attack", "DamageTaken" }, skill.chargeType) < 0)
                    errors.Add($"skill '{skill.id}': SP/chargeType 값 오류");
            }
            foreach (string key in manifest.requiredTextKeys ?? Array.Empty<string>())
                catalog.CheckText(key, manifestPath, errors);
            if (errors.Count > 0) throw new GameDataValidationException(errors);
            return catalog;
        }

        private static T Parse<T>(Func<string, string> read, string path, List<string> errors) where T : class
        {
            try
            {
                string json = read(path);
                if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("파일 누락 또는 빈 JSON");
                JsonSyntaxGuard.Validate(json);
                T value = JsonUtility.FromJson<T>(json);
                if (value == null) throw new InvalidOperationException("JSON 객체가 없습니다.");
                return value;
            }
            catch (Exception e) { errors.Add($"{path}: {e.Message}"); return null; }
        }

        private void AddTable<T>(Func<string, string> read, string path, Dictionary<string, T> target,
            Func<T, string> keyOf, List<string> errors) where T : class
        {
            DataTable<T> table = Parse<DataTable<T>>(read, path, errors);
            if (table == null) return;
            if (table.schemaVersion != 1) errors.Add($"{path}: schemaVersion은 1이어야 합니다.");
            if (table.entries == null) { errors.Add($"{path}: entries 배열 누락"); return; }
            foreach (T entry in table.entries)
            {
                string key = entry == null ? null : keyOf(entry);
                if (key == null || !IdPattern.IsMatch(key)) { errors.Add($"{path}: 유효하지 않은 ID/키 '{key}'"); continue; }
                if (target.ContainsKey(key)) errors.Add($"{path}: 중복 ID/키 '{key}'");
                else target.Add(key, entry);
            }
        }

        private void CheckText(string key, string owner, List<string> errors)
        {
            if (key == null || !texts.ContainsKey(key)) errors.Add($"'{owner}': 누락 text 참조 '{key}'");
        }

        private static void ValidateStats(StatDefinition stats, List<string> errors)
        {
            if (stats.maxHP <= 0 || stats.attack < 0 || stats.defense < 0 || stats.artsResistance < 0 ||
                stats.attackSpeed <= 0f || float.IsNaN(stats.attackSpeed) || float.IsInfinity(stats.attackSpeed))
                errors.Add($"'{stats.id}': 기본 스탯 누락 또는 범위 오류");
        }

        public bool ContainsText(string key) => key != null && texts.ContainsKey(key);
        public bool ContainsDefinition(string kind, string id) => id != null &&
            (kind == "operator" ? operators.ContainsKey(id) : kind == "enemy" ? enemies.ContainsKey(id) : kind == "skill" && skills.ContainsKey(id));
        public string Text(string key) => Get(texts, key, "text").value;
        public OperatorDefinition Operator(string id) => Get(operators, id, "operator");
        public EnemyDefinition Enemy(string id) => Get(enemies, id, "enemy");
        public SkillDefinition Skill(string id) => Get(skills, id, "skill");
        private static T Get<T>(Dictionary<string, T> source, string id, string kind)
        {
            if (id != null && source.TryGetValue(id, out T value)) return value;
            throw new KeyNotFoundException($"[GameData] 누락 {kind} 참조 '{id}'");
        }
    }
}
