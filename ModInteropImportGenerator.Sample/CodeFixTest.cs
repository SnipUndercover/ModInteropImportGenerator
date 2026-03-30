using ModInteropImportGenerator.Sample.Stubs;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ModInteropImportGenerator.Sample
{
    class Nest
    {
        [GenerateImports("CommunalHelper.DashStates")]
        public static class DashStates1
        {
            #region DreamTunnel

            public static int GetDreamTunnelDashState()
            {
                return 0;
            }

            public static bool HasDreamTunnelDash()
            {
                return false;
            }

            public static int GetDreamTunnelDashCount()
            {
                return 0;
            }

            public static ComponentStub DreamTunnelInteraction(
                Action<PlayerStub> onPlayerEnter,
                Action<PlayerStub> onPlayerExit)
            {
                return null;
            }

            #endregion

            #region Seeker

            public static bool HasSeekerDash()
            {
                return false;
            }

            public static bool IsSeekerDashAttacking()
            {
                return false;
            }

            #endregion
        }

    }
}
