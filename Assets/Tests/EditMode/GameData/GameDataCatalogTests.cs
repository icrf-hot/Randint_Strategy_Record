using System.Collections.Generic;
using NUnit.Framework;
using Randint.Data;

public class GameDataCatalogTests
{
    private Dictionary<string, string> files;

    [SetUp]
    public void Setup()
    {
        files = new Dictionary<string, string>
        {
            ["GameData/manifest"] = "{\"schemaVersion\":1,\"locale\":\"ko-KR\",\"files\":[{\"kind\":\"operator\",\"path\":\"GameData/operators\"},{\"kind\":\"text\",\"path\":\"GameData/text\"},{\"kind\":\"enemy\",\"path\":\"GameData/enemies\"},{\"kind\":\"skill\",\"path\":\"GameData/skills\"}]}",
            ["GameData/text"] = "{\"schemaVersion\":1,\"entries\":[{\"key\":\"operator.test.name\",\"value\":\"테스트\"},{\"key\":\"operator.test.info\",\"value\":\"설명\\n다음 줄\"}]}",
            ["GameData/operators"] = "{\"schemaVersion\":1,\"entries\":[{\"id\":\"test\",\"nameKey\":\"operator.test.name\",\"infoKey\":\"operator.test.info\",\"position\":\"Front\",\"classId\":\"Defender\",\"maxHP\":100,\"attack\":20,\"defense\":10,\"artsResistance\":0,\"attackSpeed\":1,\"skillIds\":[]}]}",
            ["GameData/enemies"] = "{\"schemaVersion\":1,\"entries\":[]}",
            ["GameData/skills"] = "{\"schemaVersion\":1,\"entries\":[]}"
        };
    }

    private GameDataCatalog Load() => GameDataCatalog.Load(path => files.TryGetValue(path, out string json) ? json : null);

    [Test] public void ValidDataResolvesForwardReferencesAndKoreanText()
    {
        GameDataCatalog data = Load();
        Assert.That(data.Operator("test").maxHP, Is.EqualTo(100));
        Assert.That(data.Text("operator.test.info"), Is.EqualTo("설명\n다음 줄"));
        Assert.Throws<KeyNotFoundException>(() => data.Text("missing"));
    }

    [Test] public void DuplicateTextAcrossFilesIsRejected()
    {
        files["GameData/manifest"] = files["GameData/manifest"].Replace("\"files\":[", "\"files\":[{\"kind\":\"text\",\"path\":\"GameData/other\"},");
        files["GameData/other"] = files["GameData/text"];
        Assert.That(Assert.Throws<GameDataValidationException>(() => Load()).Message, Does.Contain("중복 ID/키"));
    }

    [Test] public void DuplicateDefinitionIdIsRejected()
    {
        files["GameData/enemies"] = "{\"schemaVersion\":1,\"entries\":[{\"id\":\"test\"},{\"id\":\"test\"}]}";
        Assert.That(Assert.Throws<GameDataValidationException>(() => Load()).Message, Does.Contain("중복 ID/키"));
    }

    [TestCase("operator.test.name", "missing.text", "누락 text 참조")]
    [TestCase("\"skillIds\":[]", "\"skillIds\":[\"missing_skill\"]", "누락 skill 참조")]
    [TestCase("\"maxHP\":100,", "", "기본 스탯")]
    [TestCase("\"id\":\"test\"", "\"id\":\"Test bad\"", "유효하지 않은 ID")]
    [TestCase("\"classId\":\"Defender\"", "\"classId\":\"Caster\"", "조합 오류")]
    public void InvalidDefinitionsAreRejected(string from, string to, string message)
    {
        files["GameData/operators"] = files["GameData/operators"].Replace(from, to);
        Assert.That(Assert.Throws<GameDataValidationException>(() => Load()).Message, Does.Contain(message));
    }

    [Test] public void MissingFileReportsItsPath()
    {
        files.Remove("GameData/text");
        Assert.That(Assert.Throws<GameDataValidationException>(() => Load()).Message, Does.Contain("GameData/text"));
    }

    [Test] public void MalformedJsonIsRejected()
    {
        files["GameData/text"] = "{not json";
        Assert.Throws<GameDataValidationException>(() => Load());
    }

    [Test] public void ExplicitlyUnwrittenTextIsAllowedButMissingValueIsNot()
    {
        files["GameData/text"] = files["GameData/text"].Replace("\"value\":\"테스트\"", "\"value\":\"\",\"allowEmpty\":true");
        Assert.That(Load().Text("operator.test.name"), Is.Empty);
        files["GameData/text"] = files["GameData/text"].Replace("\"value\":\"\",", "");
        Assert.Throws<GameDataValidationException>(() => Load());
    }

    [TestCase("{\"schemaVersion\":1,\"schemaVersion\":1,\"entries\":[]}")]
    [TestCase("{\"schemaVersion\":1,\"entries\":[],}")]
    [TestCase("{\"schemaVersion\":1,\"entries\":[]} trailing")]
    public void DuplicatePropertiesAndInvalidSyntaxAreRejected(string json)
    {
        files["GameData/enemies"] = json;
        Assert.Throws<GameDataValidationException>(() => Load());
    }

    [Test] public void MigratedResourcesPassValidation()
    {
        GameDataCatalog data = GameDataCatalog.Load(path =>
        {
            UnityEngine.TextAsset asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(path);
            return asset == null ? null : asset.text;
        });
        Assert.That(data.Operator("saria").maxHP, Is.EqualTo(3150));
        Assert.That(data.Enemy("makeshift").attack, Is.EqualTo(2000));
        Assert.That(data.Text("battle.result.success.title"), Is.EqualTo("데이터 통과됨"));
        Assert.That(data.Text("dialogue.battle_result.success"), Is.Empty);
    }
}
