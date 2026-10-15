using System;
using UnityEngine;

// Verification-only in-memory overrides; never writes the source JSON or assets.
public sealed class JsonDefinitionScope : IDisposable
{
    private readonly object definition;
    private readonly string original;
    public JsonDefinitionScope(object definition)
    {
        this.definition = definition;
        original = JsonUtility.ToJson(definition);
    }
    public void Dispose() => JsonUtility.FromJsonOverwrite(original, definition);
}
