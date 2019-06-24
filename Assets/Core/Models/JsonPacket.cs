using Assets.Core.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Core.Models
{
    public class JsonPacket
    {
        public const byte INITIALIZER = 0x01;
        public const byte FINALIZER = 0x04;

        public string ClientID { get; set; }
        public int PacketID { get; set; }
        public ContentResult ContentResult { get; set; }
        public string ContentType { get; set; }
        public string ContentJson { get; set; }


        public JsonPacket(string clientId, string contentType, string contentJson)
        {
            ClientID = clientId;
            ContentType = contentType;
            ContentJson = contentJson;
            PacketID = (int)(DateTime.UtcNow - DateTime.UtcNow.Date).TotalMilliseconds;
        }

        protected JsonPacket(string clientId, string contentType, string contentJson, ContentResult contentResult, int packetId)
        {
            ClientID = clientId;
            ContentType = contentType;
            ContentJson = contentJson;
            PacketID = packetId;
            ContentResult = contentResult;
        }

        public T DeserializeContent<T>()
        {
            return JsonConvert.DeserializeObject<T>(ContentJson);
        }

        public bool TryDeserializeContent<T>(out T t, T @default = default(T))
        {
            t = @default;

            try
            {
                t = JsonConvert.DeserializeObject<T>(ContentJson);
                return true;
            }
            catch(Exception e)
            {
                Debug.LogWarningFormat("TryDeserializeContent fail: {0}\r\n\n{1}", ContentJson ?? "null", e);
            }

            return false;
        }

        public byte[] GetPacket()
        {
            BinaryContentCreator payload = new BinaryContentCreator();

            payload.AddString(ClientID ?? "");
            payload.AddInt(PacketID, 4);
            payload.AddInt((int)ContentResult, 1);
            payload.AddString(ContentType ?? "");
            payload.AddString(ContentJson);

            BinaryContentCreator packet = new BinaryContentCreator();
            byte[] payloadArray = payload.GetContent();

            packet.AddInt(INITIALIZER, 1); // 1 byte
            packet.AddInt(payloadArray.Length); // 4 bytes
            packet.AddInt(0, 1); // 1 byte reserved to use in the future to indicate if payload is zipped
            packet.AddRawBytes(payloadArray); // x bytes
            packet.AddBytesCRC(payloadArray); // 1 byte
            packet.AddInt(FINALIZER, 1); // 1 byte

            return packet.GetContent();
        }

        public static bool DeserializePacket(List<byte> buffer, out JsonPacket p)
        {
            int initializerIdx = 0;
            p = null;

            if (buffer.Count <= 6)
                return false;

            // Search for initializer

            while ((initializerIdx < buffer.Count) && (buffer[initializerIdx] != INITIALIZER))
                initializerIdx++;

            if (initializerIdx > 0)
            {
                Debug.LogFormat("{0} bytes of trush removed", initializerIdx);
                buffer.RemoveRange(0, initializerIdx);
                initializerIdx = 0;
            }

            if (initializerIdx >= buffer.Count) // Only trush in buffer
                return false;

            // Check size

            BinaryContentReader reader = new BinaryContentReader(buffer.ToArray());

            reader.ReadInt(1); // INITIALIZER
            int payloadsize = reader.ReadInt(4);
            reader.ReadInt(1); // 1 byte reserved to use in the future to indicate if payload is zipped

            if ((payloadsize + 7) >= buffer.Count) // Not enought, wait for more data
                return false;

            // Check CRC and finalizer

            byte[] payload = reader.ReadRawBytes(payloadsize);
            reader.ReadAndVerifyBytesCRC(payload);

            if (reader.ReadInt(1) != FINALIZER)
            {
                Debug.LogErrorFormat("UNEXPECTED ERROR! {0} bytes removed, finalizer wasn't in the correct place", reader.CurrentIndex);
                buffer.RemoveRange(0, reader.CurrentIndex);
                return false;
            }

            buffer.RemoveRange(0, reader.CurrentIndex);

            // Decode payload

            reader = new BinaryContentReader(payload);

            string clientId = reader.ReadString();
            int packetId = reader.ReadInt(4);
            ContentResult result = (ContentResult)reader.ReadInt(1);
            string contentType = reader.ReadString();
            string json = reader.ReadString();

            p = new JsonPacket(clientId, contentType, json, result, packetId);

            return true;
        }

        public override string ToString()
        {
            return string.Format("ClientID: {0}, PacketID: {1}, URI: {2}, Result: {3}, Content: {4}",
                ClientID,
                PacketID.ToString("X8"),
                ContentType,
                ContentResult,
                ContentJson);
        }
    }
}
