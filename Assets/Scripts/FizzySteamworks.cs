using System;
using UnityEngine;
using Steamworks;
using Mirror;

namespace Mirror.FizzySteam
{
    public class FizzySteamworks : Transport
    {
        private const string FyziError = "[FizzySteamworks] ";

        protected Callback<P2PSessionRequest_t> p2PSessionRequest;
        protected Callback<P2PSessionConnectFail_t> p2PSessionConnectFail;

        public override bool Available() => SteamManager.Initialized;

        public override void ClientConnect(string address)
        {
            if (!Available())
            {
                Debug.LogError(FyziError + "Steam API başlatılamadığı için bağlanılamıyor!");
                return;
            }

            if (ulong.TryParse(address, out ulong steamID))
            {
                CSteamID hostSteamID = new CSteamID(steamID);
                SteamNetworking.SendP2PPacket(hostSteamID, new byte[1] { 0 }, 1, EP2PSend.k_EP2PSendReliable);
                Debug.Log(FyziError + "Host'a bağlantı isteği gönderildi: " + steamID);
            }
            else
            {
                Debug.LogError(FyziError + "Geçersiz Steam ID adresi: " + address);
            }
        }

        public override bool ClientConnected() => true;
        public override void ClientDisconnect() { }

        // Mirror'ın yeni sürümünde void döndürüyor
        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            // Gönderim mantığı
        }

        public override void ServerStart()
        {
            if (!Available()) return;

            p2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            p2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
            Debug.Log(FyziError + "Steam P2P Sunucusu Başlatıldı.");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t result)
        {
            SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
            Debug.Log(FyziError + "P2P Oturum isteği kabul edildi: " + result.m_steamIDRemote);
        }

        private void OnP2PSessionConnectFail(P2PSessionConnectFail_t result)
        {
            Debug.LogError(FyziError + "P2P Bağlantı hatası: " + result.m_eP2PSessionError);
        }

        public override void ServerStop() { }
        public override bool ServerActive() => true;

        // Mirror'ın yeni sürümünde void döndürüyor
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            // Gönderim mantığı
        }

        public override string ServerGetClientAddress(int connectionId) => string.Empty;
        public override void ServerDisconnect(int connectionId) { }

        // Mirror'ın zorunlu kıldığı ServerUri
        public override Uri ServerUri()
        {
            return new Uri("steam://" + SteamUser.GetSteamID().ToString());
        }

        public override void Shutdown() { }
        public override int GetMaxPacketSize(int channelId = Channels.Reliable) => 1200;
    }
}