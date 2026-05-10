using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Invokes BootstrapRuntime game-over without a compile-time type reference (HeroKnight must always compile).
/// Uses AddComponent("BootstrapRuntime") when reflection fails — same as the Editor does for script names.
/// </summary>
public static class GameOverNotifier
{
    const string ScriptClassName = "BootstrapRuntime";
    const string HostObjectName = "BootstrapRuntime";
    const string ShowMethodMessage = "ShowGameOverScreen";

    public static void Raise()
    {
        GameObject host = GameObject.Find(HostObjectName);
        Component bootstrap = host != null ? host.GetComponent(ScriptClassName) : null;

        if (bootstrap == null)
        {
            if (host == null)
            {
                host = new GameObject(HostObjectName);
                UnityEngine.Object.DontDestroyOnLoad(host);
            }

            Type resolved = FindBootstrapComponentType();
            bootstrap = resolved != null ? host.AddComponent(resolved) : host.AddComponent(ScriptClassName);
        }

        if (bootstrap == null)
        {
            if (host != null && host.transform.childCount == 0 &&
                host.GetComponents<Component>().Length <= 2)
                UnityEngine.Object.Destroy(host);

            ProfessorFallbackUi.ShowGameOverBoard();
            return;
        }

        TryInvokeEnsureHostExists(bootstrap.GetType());
        host.SendMessage(ShowMethodMessage, SendMessageOptions.RequireReceiver);
    }

    static void TryInvokeEnsureHostExists(Type bootstrapType)
    {
        if (bootstrapType == null)
            return;

        MethodInfo ensure = bootstrapType.GetMethod(
            "EnsureHostExists",
            BindingFlags.Public | BindingFlags.Static);

        if (ensure != null)
            ensure.Invoke(null, null);
    }

    static Type FindBootstrapComponentType()
    {
        string[] simpleNames =
        {
            ScriptClassName,
            "BootstrapRuntime",
        };

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (string name in simpleNames)
            {
                Type direct = asm.GetType(name);
                if (direct != null && typeof(Component).IsAssignableFrom(direct))
                    return direct;
            }

            Type qualified = asm.GetType(ScriptClassName + ", Assembly-CSharp");
            if (qualified != null && typeof(Component).IsAssignableFrom(qualified))
                return qualified;

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
                    t.Name == ScriptClassName &&
                    typeof(Component).IsAssignableFrom(t))
                    return t;
            }
        }

        return null;
    }
}
