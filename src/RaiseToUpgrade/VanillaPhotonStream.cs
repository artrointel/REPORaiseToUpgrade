using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;

namespace RaiseToUpgrade;

internal static class VanillaPhotonStream
{
    private static readonly FieldInfo WriteData = typeof(PhotonStream).GetField(
        "writeData", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PhotonStream).FullName, "writeData");

    internal static PhotonStream CreateWriter()
    {
        // The game's PUN constructor does not allocate its write buffer.
        // Initialize it before any observed component calls SendNext.
        PhotonStream stream = new(true, null);
        WriteData.SetValue(stream, new List<object>());
        return stream;
    }
}
