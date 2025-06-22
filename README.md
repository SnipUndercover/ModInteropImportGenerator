# ModInteropImportGenerator

> [!NOTE]
> Currently WIP. I just put the repository so that I don't lose it in the future.  
> *(and also to share, i guess)*

Generates `MonoMod.ModInterop` imports based on a given method signature and name.

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

Instead of doing the following, with the slightly confusing delegate function types...

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

... you can simply copy paste the method signatures, cut out the method bodies and mark them as partial!  
The source generator will figure out the rest.

```cs
[GenerateImports("CommunalHelper.DashStates")]
public static partial class DashStates
{
    public static partial int GetDreamTunnelDashState();
    public static partial bool HasDreamTunnelDash();
    public static partial int GetDreamTunnelDashCount();
    public static partial Component DreamTunnelInteraction(
        Action<Player> onPlayerEnter,
        Action<Player> onPlayerExit);
    public static partial bool HasSeekerDash();
    public static partial bool IsSeekerDashAttacking();
}
```

> [!IMPORTANT]
> The [GenerateImports] attribute also generates a `Load` method. Remember to call it in your module's `Load` call!

The `GenerateImports` attribute also allows specifying whether the `ModInterop` dependency is optional or required, with `RequiredDependency`.  
If set to required, an exception will be thrown on `Load` if any of the defined `ModInterop` methods are not loaded.

