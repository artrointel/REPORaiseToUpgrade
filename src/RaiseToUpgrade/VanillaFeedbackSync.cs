using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace RaiseToUpgrade;

internal static class VanillaFeedbackSync
{
    // PUN's existing reliable serialization event; vanilla clients already handle it.
    internal const byte ReliableSerializationEvent = 206;

    internal static void SendFirstSnapshot(PhotonView view)
    {
        if (!PhotonNetwork.IsMasterClient || view.ViewID <= 0 ||
            view.Synchronization != ViewSynchronization.Off)
        {
            throw new InvalidOperationException("Feedback requires an unsynchronized host-owned carrier.");
        }

        int timestamp = PhotonNetwork.ServerTimestamp;
        PhotonStream stream = VanillaPhotonStream.CreateWriter();
        view.SerializeView(stream, new PhotonMessageInfo(PhotonNetwork.LocalPlayer, timestamp, view));
        object[] payload = stream.ToArray();
        if (payload.Length == 0)
        {
            throw new InvalidOperationException("The feedback carrier has no observed state.");
        }

        object[] snapshot = NativeSnapshotPacket.Build(timestamp, view.Prefix, view.ViewID, payload);
        bool sent = PhotonNetwork.NetworkingClient.OpRaiseEvent(
            ReliableSerializationEvent,
            snapshot,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, InterestGroup = view.Group },
            SendOptions.SendReliable);
        if (!sent)
        {
            throw new InvalidOperationException("Could not send the vanilla feedback snapshot.");
        }
    }
}
