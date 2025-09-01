# ModInteropImportGenerator

Generates `MonoMod.ModInterop` imports based on a given method signature and name.

<!-- TOC -->
* [ModInteropImportGenerator](#modinteropimportgenerator)
  * [Terminology](#terminology)
  * [Demonstration](#demonstration)
  * [Referencing](#referencing)
  * [Usage](#usage)
  * [Building](#building)
<!-- TOC -->

## Terminology

| Term            | Definition                                                                                                                                                                                       |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| *Dependency*    | An assembly that includes one or more *export classes*.                                                                                                                                          |
| *Export class*  | A class annotated with `[ModExportName]` that contains one or more *export methods*.                                                                                                             |
| *Export name*   | The name passed to the `[ModExportName]` annotation.                                                                                                                                             |
| *Export method* | A `public static` method inside an *export class*.                                                                                                                                               |
| *Import class*  | A class annotated with `[GenerateImports]` that contains one or more *import methods*.                                                                                                           |
| *Import name*   | The name passed to the `[GenerateImports]` annotation.                                                                                                                                           |
| *Import method* | A `public static partial` method inside an *import class* that does not have a method body.                                                                                                      |
| *Import*        | The process of assigning the correct *export method* implementation to *import methods* of a given *import class*, based on the *import name* as well as the *import method* name and signature. |

## Demonstration

Assume the following export class:

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

Normally, to import it, you would define your import class like so, and call `typeof(DashStates).ModInterop();`:

```cs
[ModImportName("CommunalHelper.DashStates")]
public static class DashStates
{
    public static Func<int> GetDreamTunnelDashState;
    public static Func<bool> HasDreamTunnelDash;
    public static Func<int> GetTunnelDashCount;
    public static Func<Action<Player>, Action<Player>, Component>
        DreamTunnelInteraction;
    public static Func<bool> HasSeekerDash;
    public static Func<bool> IsSeekerDashAttacking;
}
```

This form can be confusing for new code modders and may sometimes be difficult to read.

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

- The definition is more readable *(literally just a function)*
- You don't have to convert function signatures into `Func<...>`s or `Action<...>`s, or define your own delegate types
- You get parameter names as a bonus
- You don't have to constantly slap an `?.Invoke(...)` on the imported methods if the dependency is optional

## Referencing

> [!NOTE]
> The source generator is planned to eventually be published as a NuGet package.
> For the time being, you'll need to compile the source generator yourself &ndash; see the build instructions at the bottom.

Edit your mod's `.csproj`. Inside an `<ItemGroup>`, add the `<Analyzer>` tag, with  the `Include` path
set to the built source generator DLL.

```xml
<ItemGroup>
    <Analyzer Include="path/to/ModInteropImportGenerator.dll" />
</ItemGroup>
```

## Usage

Importing methods is done in a very similar fashion to how it was previously done with `[ModImportName]` and
`typeof(...).ModInterop();`.

Define a `public static partial` class and give it the `[GenerateImports]` attribute. Make sure the import name matches
the export name you're interested in.

```cs
// DashStates.cs

using MonoModImportGenerator;

[GenerateImports("CommunalHelper.DashStates")]
public static partial class DashStates
{
}
```

Next, define import methods that have the same name and signature as the export methods that you're interested in.  
The easiest way would be to copy-paste the export method definitions, leave out the method bodies and mark them
as `partial` *(and of course add the semicolon at the end)*.

```cs
// DashStates.cs

using MonoModImportGenerator;

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

Finally, call the `Load()` method on the import class. This method is automatically generated by the source generator.

```cs
// YourModModule.cs

public override void Load()
{
    DashStates.Load();
    
    // ...
}
```

You may now call the import methods.  
By default, the source generator will treat the import class as an optional dependency.
If the dependency is not present at the time `Load()` is called, the import methods will throw an exception when called.

To safely call an import method, you must check if the `IsImported` `bool` property is `true`.

```cs
if (DashStates.IsImported)
    DashStates.GetDreamTunnelDashState();
```

You can tell the source generator to treat the import class as a required dependency by setting the `RequiredDependency`
property to `true`. This will throw an exception directly in `Load()` if any methods fail to import.

```cs
// DashStates.cs

using MonoModImportGenerator;

[GenerateImports("CommunalHelper.DashStates", RequiredDependency = true)]
public static partial class DashStates
{
    // ...
}
```

You can see the state of the import in the `ImportState` property of the import class.

## Building

Clone the project and build the `ModInteropImportGenerator` project. Prefer Release mode as it's
more optimized.
