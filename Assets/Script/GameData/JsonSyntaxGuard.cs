using System;
using System.Collections.Generic;
using System.Globalization;

namespace Randint.Data
{
    // JsonUtility가 조용히 덮어쓸 수 있는 중복 JSON 속성도 역직렬화 전에 차단합니다.
    internal sealed class JsonSyntaxGuard
    {
        private readonly string json;
        private int index;
        private JsonSyntaxGuard(string json) { this.json = json; }
        public static void Validate(string json)
        {
            var parser = new JsonSyntaxGuard(json);
            parser.Space();
            if (parser.Peek() != '{') parser.Fail("최상위 JSON은 객체여야 합니다.");
            parser.Value(0);
            parser.Space();
            if (parser.index != json.Length) parser.Fail("JSON 뒤에 불필요한 내용이 있습니다.");
        }

        private void Value(int depth)
        {
            if (depth > 64) Fail("JSON 중첩 한도 초과");
            Space();
            char c = Peek();
            if (c == '{')
            {
                index++;
                var names = new HashSet<string>(StringComparer.Ordinal);
                Space();
                if (Take('}')) return;
                do
                {
                    Space();
                    string name = String();
                    if (!names.Add(name)) Fail($"중복 JSON 속성 '{name}'");
                    Space(); Require(':'); Value(depth + 1); Space();
                    if (Take('}')) return;
                    Require(',');
                } while (true);
            }
            if (c == '[')
            {
                index++; Space();
                if (Take(']')) return;
                do
                {
                    Value(depth + 1); Space();
                    if (Take(']')) return;
                    Require(',');
                } while (true);
            }
            if (c == '"') { String(); return; }
            foreach (string literal in new[] { "true", "false", "null" })
            {
                if (index + literal.Length <= json.Length && string.CompareOrdinal(json, index, literal, 0, literal.Length) == 0)
                { index += literal.Length; return; }
            }
            Number();
        }

        private string String()
        {
            Require('"');
            var text = new System.Text.StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') return text.ToString();
                if (c < 32) Fail("문자열에 이스케이프되지 않은 제어 문자가 있습니다.");
                if (c != '\\') { text.Append(c); continue; }
                if (index == json.Length) Fail("문자열 escape가 끝나지 않았습니다.");
                c = json[index++];
                switch (c)
                {
                    case '"': case '\\': case '/': text.Append(c); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length) Fail("불완전한 Unicode escape");
                        if (!ushort.TryParse(json.Substring(index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code))
                            Fail("잘못된 Unicode escape");
                        text.Append((char)code); index += 4; break;
                    default: Fail("지원하지 않는 문자열 escape"); break;
                }
            }
            Fail("문자열이 끝나지 않았습니다.");
            return null;
        }

        private void Number()
        {
            Take('-');
            if (!Take('0'))
            {
                if (Peek() < '1' || Peek() > '9') Fail("유효한 JSON 값이 아닙니다.");
                Digits();
            }
            if (Take('.')) { RequireDigit(); Digits(); }
            if (Take('e') || Take('E'))
            {
                if (!Take('+')) Take('-');
                RequireDigit(); Digits();
            }
        }

        private void RequireDigit() { if (Peek() < '0' || Peek() > '9') Fail("숫자가 필요합니다."); }
        private void Digits() { while (Peek() >= '0' && Peek() <= '9') index++; }
        private void Space() { while (index < json.Length && (json[index] == ' ' || json[index] == '\t' || json[index] == '\r' || json[index] == '\n')) index++; }
        private char Peek() => index < json.Length ? json[index] : '\0';
        private bool Take(char c) { if (Peek() != c) return false; index++; return true; }
        private void Require(char c) { if (!Take(c)) Fail($"'{c}'가 필요합니다."); }
        private void Fail(string message) => throw new FormatException($"JSON 위치 {index}: {message}");
    }
}
