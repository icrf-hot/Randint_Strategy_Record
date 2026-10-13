using System;
using System.Reflection;
using UnityEngine;

public static class CardSelectionLifecycleVerification
{
    public static void Run()
    {
        Verify<FrontCardSelectionManager>();
        Verify<MiddleCardSelectionManager>();
        Verify<BackCardSelectionManager>();
        Debug.Log("[Card Selection Lifecycle Verification] Owner cleanup, non-owner safety and replacement passed for all three positions.");
    }

    private static void Verify<T>() where T : CardSelectionManagerBase
    {
        Type type = typeof(T);
        PropertyInfo instance = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        MethodInfo awake = type.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo destroy = type.GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
        if (instance == null || awake == null || destroy == null ||
            !ReferenceEquals(instance.GetValue(null), null))
            throw new InvalidOperationException(type.Name + " lifecycle requires a clean test context.");

        GameObject ownerObject = new GameObject(type.Name + "_Owner");
        GameObject otherObject = new GameObject(type.Name + "_Other");
        ownerObject.SetActive(false);
        otherObject.SetActive(false);
        T owner = ownerObject.AddComponent<T>();
        T other = otherObject.AddComponent<T>();
        try
        {
            // Invoke lifecycle hooks explicitly; this test does not enter Play Mode.
            awake.Invoke(owner, null);
            if (!ReferenceEquals(instance.GetValue(null), owner))
                throw new InvalidOperationException(type.Name + " did not register its owner.");

            destroy.Invoke(other, null);
            if (!ReferenceEquals(instance.GetValue(null), owner))
                throw new InvalidOperationException(type.Name + " non-owner cleared the singleton.");

            destroy.Invoke(owner, null);
            if (!ReferenceEquals(instance.GetValue(null), null))
                throw new InvalidOperationException(type.Name + " retained its destroyed owner reference.");

            awake.Invoke(other, null);
            if (!ReferenceEquals(instance.GetValue(null), other))
                throw new InvalidOperationException(type.Name + " failed to register a replacement.");
        }
        finally
        {
            destroy.Invoke(owner, null);
            destroy.Invoke(other, null);
            UnityEngine.Object.DestroyImmediate(ownerObject);
            UnityEngine.Object.DestroyImmediate(otherObject);
        }
    }
}
