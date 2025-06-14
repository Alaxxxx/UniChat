using System;
using System.Text;

namespace UniChat.Editor.Core
{
      public static class ChatProtocol
      {
            private const string MsgPrefix = "MSG:";
            private const string FilePrefix = "FILE:";

            public static byte[] EncodeTextMessage(string author, string message)
            {
                  return Encoding.UTF8.GetBytes($"{MsgPrefix}{author}:{message}");
            }

            public static byte[] EncodeFileChunk(string fileName, int chunkIndex, int totalChunks, byte[] chunkData)
            {
                  byte[] metadata = Encoding.UTF8.GetBytes($"{FilePrefix}{fileName}:{chunkIndex}:{totalChunks}:\0");
                  byte[] packet = new byte[metadata.Length + chunkData.Length];
                  Buffer.BlockCopy(metadata, 0, packet, 0, metadata.Length);
                  Buffer.BlockCopy(chunkData, 0, packet, metadata.Length, chunkData.Length);

                  return packet;
            }

            public static object ParseReceivedData(byte[] buffer, int bytesRead)
            {
                  string receivedString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                  if (receivedString.StartsWith(MsgPrefix, StringComparison.Ordinal))
                  {
                        return receivedString[MsgPrefix.Length..];
                  }

                  if (!receivedString.StartsWith(FilePrefix, StringComparison.Ordinal))
                  {
                        return null;
                  }

                  int metadataEndIndex = receivedString.IndexOf('\0');

                  if (metadataEndIndex == -1)
                  {
                        return null;
                  }

                  string metadataStr = receivedString[..metadataEndIndex];
                  string[] parts = metadataStr.Split(':');

                  if (parts.Length < 4)
                  {
                        return null;
                  }

                  var fileInfo = new
                  {
                              FileName = parts[1],
                              ChunkIndex = int.Parse(parts[2]),
                              TotalChunks = int.Parse(parts[3]),
                              DataOffset = metadataEndIndex + 1
                  };

                  return fileInfo;

            }
      }
}