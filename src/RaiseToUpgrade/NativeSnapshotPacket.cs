using System;

namespace RaiseToUpgrade;

internal static class NativeSnapshotPacket
{
    internal static object[] Build(int timestamp, int prefix, int viewId, object[] payload)
    {
        if (viewId <= 0 || prefix < 0 || prefix > byte.MaxValue || payload.Length == 0)
        {
            throw new ArgumentException("Invalid native view snapshot.");
        }

        // PUN OnSerializeRead expects [view ID, delta flag, null-delta indices,
        // observed component values...], wrapped in [timestamp, level prefix, view].
        object[] viewData = new object[payload.Length + 3];
        viewData[0] = viewId;
        viewData[1] = false;
        viewData[2] = null!;
        Array.Copy(payload, 0, viewData, 3, payload.Length);
        return new object[] { timestamp, prefix == 0 ? null! : (object)(byte)prefix, viewData };
    }
}
