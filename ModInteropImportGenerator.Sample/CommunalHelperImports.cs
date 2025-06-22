using System;

namespace ModInteropImportGenerator.Sample;

[GenerateImports("CommunalHelper.DashStates")]
public static partial class DashStates
{
    public static partial int GetDreamTunnelDashState();
    public static partial bool HasDreamTunnelDash();
    public static partial int GetDreamTunnelDashCount();
    public static partial ComponentStub DreamTunnelInteraction(
        Action<PlayerStub> onPlayerEnter,
        Action<PlayerStub> onPlayerExit);
    public static partial bool HasSeekerDash();
    public static partial bool IsSeekerDashAttacking();
}
