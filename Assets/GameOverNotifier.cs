using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Invokes game-over UI on BootstrapRuntime without a compile-time reference to that type,
/// so HeroKnight and the rest of the game compile even if BootstrapRuntime is missing or out of sync (CS0246).
/// </summary>
public static class GameOverNotifier
{
    const string BootstrapTypeName = "BootstrapRuntime";
    const string HostObjectName = "BootstrapRuntime";
    const string ShowMethodMessage = "ShowGameOverScreen";

    public static void Raise()
    {
        Type bootstrapType = FindBootstrapComponentType();

        if (bootstrapType != null)
        {
            MethodInfo ensure = bootstrapType.GetMethod(
                "EnsureHostExists",
                BindingFlags.Public | BindingFlags.Static);

            if (ensure != null)
                ensure.Invoke(null, null);
        }

        GameObject host = GameObject.Find(HostObjectName);

        if (host == null && bootstrapType != null)
        {
            host = new GameObject(HostObjectName);
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent(bootstrapType);
        }

        if (host != null)
        {
            host.SendMessage(ShowMethodMessage, SendMessageOptions.RequireReceiver);
            return;
        }

        Debug.LogError(
            "Game over UI cannot show: BootstrapRuntime was not found. " +
            "Restore Assets/BootstrapRuntime.cs from the repo (branch adel-al-ashi).");

        Time.timeScale = 0f;
    }

    static Type FindBootstrapComponentType()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type direct = asm.GetType(BootstrapTypeName);
            if (direct != null && typeof(Component).IsAssignableFrom(direct))
                return direct;

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                continue;
            }

            if (types == null)
                continue;

            foreach (Type t in types)
            {
                if (t != null &&
                    t.Name == BootstrapTypeName &&
                    typeof(Component).IsAssignableFrom(t))
                    return t;
            }
        }

        return null;
    }
}
