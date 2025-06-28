# ModInteropImportGenerator

> [!NOTE]
> Currently WIP. I just put the repository so that I don't lose it in the future.  
> *(and also to share, i guess)*

Generates `MonoMod.ModInterop` imports based on a given method signature and name.

## Example

Assume the following mod export:

```cs
[ModExportName("CommunalHelper.DashStates")]
public static class DashStates
{
    #region DreamTunnel

    public static int GetDreamTunnelDashState()
    {
        return St.DreamTunnelDash;
    }

    public static bool HasDreamTunnelDash()
    {
        return DreamTunnelDash.DreamTunnelDashCount > 0;
    }

    public static int GetDreamTunnelDashCount()
    {
        return DreamTunnelDash.DreamTunnelDashCount;
    }

    public static Component DreamTunnelInteraction(
        Action<Player> onPlayerEnter,
        Action<Player> onPlayerExit)
    {
        return new DreamTunnelInteraction(onPlayerEnter, onPlayerExit);
    }

    #endregion

    #region Seeker

    public static bool HasSeekerDash()
    {
        return SeekerDash.HasSeekerDash;
    }

    public static bool IsSeekerDashAttacking()
    {
        return SeekerDash.SeekerAttacking;
    }

    #endregion
}
```

Normally, to import it, you'd define your import class like this.

```cs
[ModImportName("CommunalHelper.DashStates")]
public static class DashStates
{
    public static Func<int> GetDreamTunnelDashState;
    public static Func<bool> HasDreamTunnelDash;
    public static Func<int> GetTunnelDashCount;
    public static Func<Action<Player>, Action<Player>, Component> DreamTunnelInteraction;
    public static Func<bool> HasSeekerDash;
    public static Func<bool> IsSeekerDashAttacking;
}
```

While this works and is relatively clean, it can be confusing for new code modders and may not be the most readable thing in the world.

However, with this source generator you can now copy-paste the method signatures and mark them as `partial`!
The source generator will figure out the rest.

```cs
[GenerateImports("CommunalHelper.DashStates")]
public static partial class DashStates
{
    public static partial int GetDreamTunnelDashState();
    public static partial bool HasDreamTunnelDash();
    public static partial int GetDreamTunnelDashCount();
    public static partial Component DreamTunnelInteraction(
        Action<Player> onPlayerEnter, Action<Player> onPlayerExit);
    public static partial bool HasSeekerDash();
    public static partial bool IsSeekerDashAttacking();
}
```

- The definition is more readable _(literally just a function)_
- You don't have to convert function signatures into `Func<...>`s or `Action<...>`s
- You get parameter names as a bonus
- You don't have to constantly slap an `?.Invoke(...)` on the imported methods
  _(assuming the dependency is optional)_

> [!IMPORTANT]
> The `[GenerateImports]` attribute also generates a `Load` method.
> Remember to call it in your module's `Load` call!

## Usage

`[GenerateImports]` lets you change the way the source generator treats your class.

By default, the source generator assumes that the imported dependency is an _optional dependency_ &#x2013;
that is, if you try to call the function and the imported mod is not loaded, nothing will happen and the function will return immediately.  
If the return type is not `void`, the returned value will be the [default value](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/default-values) for that type.

Setting `RequiredDependency = true` in the attribute constructor signals to the source generator
that this is a required dependency, and it's expected to be enabled at all times.  
This will make the mod explicitly crash on load with an informative error message if _any_ of the
imports failed to load correctly:

```
One or more import definitions for "CommunalHelper.DashStates.GetDreamTunnelDashState" did not load correctly. Check the import name and method definitions.
```

## Referencing

> [!NOTE]
> The source generator is planned to eventually be published as a NuGet package.
> For the time being, you'll need to compile the source generator yourself.

Edit your mod's `.csproj`. Inside an `<ItemGroup>`, add the `<Analyzer>` tag, with  the `Include` path
set to the built source generator DLL.

```xml
<ItemGroup>
    <Analyzer Include="path/to/ModInteropImportGenerator.dll" />
</ItemGroup>
```

## Build

Simply clone the project and build the `ModInteropImportGenerator` project. Prefer Release mode as it's
more optimized.
