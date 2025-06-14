using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UniChat.Editor.Data;
using UnityEditor;
using UnityEngine;
using MessageType = UniChat.Editor.Data.MessageType;

namespace UniChat.Editor.Core
{
      public sealed class ChatService
      {
            public event Action<bool> OnConnectionStatusChanged;
            public event Action<ChatMessage> OnMessageReceived;
            public event Action<string, float> OnFileTransferProgress;

            public bool IsConnected { get; private set; }
            public List<ChatMessage> Messages { get; } = new();

            private TcpClient _client;
            private TcpListener _listener;
            private NetworkStream _networkStream;
            private CancellationTokenSource _cancellationTokenSource;

            private MemoryStream _receivingFileStream;
            private string _receivingFileName;

            public async Task StartServerAsync()
            {
                  if (IsConnected)
                  {
                        return;
                  }

                  try
                  {
                        _listener = new TcpListener(IPAddress.Any, 5000);
                        _listener.Start();
                        AddLog("Server started, waiting for connection...", MessageType.Log);

                        _client = await _listener.AcceptTcpClientAsync();
                        InitializeConnection();
                  }
                  catch (Exception e)
                  {
                        HandleException("Server start failed", e);
                  }
            }

            public async Task ConnectToServerAsync(string ipAddress)
            {
                  if (IsConnected)
                  {
                        return;
                  }

                  try
                  {
                        _client = new TcpClient();
                        AddLog($"Connecting to {ipAddress}...", MessageType.Log);
                        await _client.ConnectAsync(ipAddress, 5000);
                        InitializeConnection();
                  }
                  catch (Exception e)
                  {
                        HandleException("Connection failed", e);
                  }
            }

            private void InitializeConnection()
            {
                  _networkStream = _client.GetStream();
                  IsConnected = true;
                  _cancellationTokenSource = new CancellationTokenSource();
                  AddLog("Client connected!", MessageType.Notification);
                  OnConnectionStatusChanged?.Invoke(true);
                  _ = ReceiveMessagesAsync(_cancellationTokenSource.Token);
            }

            public async Task SendMessageAsync(string text)
            {
                  if (!IsConnected || string.IsNullOrEmpty(text))
                  {
                        return;
                  }

                  try
                  {
                        byte[] data = ChatProtocol.EncodeTextMessage(ChatPreferences.Username, text);
                        await _networkStream.WriteAsync(data, 0, data.Length);
                        AddMessage(ChatPreferences.Username, text, ChatPreferences.UserMessageColor);
                  }
                  catch (Exception e)
                  {
                        HandleException("Send failed", e);
                  }
            }

            public async Task SendFileAsync(string filePath)
            {
                  if (!IsConnected || string.IsNullOrEmpty(filePath))
                  {
                        return;
                  }

                  try
                  {
                        byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));
                        string fileName = Path.GetFileName(filePath);
                        int totalChunks = (fileData.Length + ChatPreferences.ChunkSize - 1) / ChatPreferences.ChunkSize;

                        AddLog($"Starting to send file: {fileName}", MessageType.Notification);

                        for (int i = 0; i < totalChunks; i++)
                        {
                              int offset = i * ChatPreferences.ChunkSize;
                              int currentChunkSize = Math.Min(ChatPreferences.ChunkSize, fileData.Length - offset);
                              byte[] chunkData = new byte[currentChunkSize];
                              Buffer.BlockCopy(fileData, offset, chunkData, 0, currentChunkSize);

                              byte[] packet = ChatProtocol.EncodeFileChunk(fileName, i, totalChunks, chunkData);
                              await _networkStream.WriteAsync(packet, 0, packet.Length);

                              OnFileTransferProgress?.Invoke($"Sending {fileName}", (float)(i + 1) / totalChunks);
                        }

                        AddLog($"File {fileName} sent successfully.", MessageType.Notification);
                  }
                  catch (Exception e)
                  {
                        HandleException("File send failed", e);
                  }
                  finally
                  {
                        OnFileTransferProgress?.Invoke("", -1);
                  }
            }

            private async Task ReceiveMessagesAsync(CancellationToken token)
            {
                  byte[] buffer = new byte[ChatPreferences.ChunkSize + 1024];

                  while (IsConnected && !token.IsCancellationRequested)
                  {
                        try
                        {
                              if (!_networkStream.DataAvailable)
                              {
                                    await Task.Delay(100, token);

                                    continue;
                              }

                              int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, token);

                              if (bytesRead == 0)
                              {
                                    Disconnect();

                                    break;
                              }

                              object parsedData = ChatProtocol.ParseReceivedData(buffer, bytesRead);

                              if (parsedData is string textMsg)
                              {
                                    string[] parts = textMsg.Split(new[] { ':' }, 2);
                                    AddMessage(parts[0], parts[1], ChatPreferences.UserMessageColor);
                              }
                              else if (parsedData is var _ && parsedData?.GetType().Name.Contains("AnonymousType", StringComparison.Ordinal) == true)
                              {
                                    dynamic info = parsedData;
                                    await HandleReceivedFileChunk(buffer, bytesRead, info.FileName, info.ChunkIndex, info.TotalChunks, info.DataOffset, token);
                              }
                        }
                        catch (OperationCanceledException)
                        {
                              break;
                        }
                        catch (Exception e)
                        {
                              HandleException("Receive error", e);

                              break;
                        }
                  }
            }

            private async Task HandleReceivedFileChunk(byte[] buffer, int bytesRead, string fileName, int chunkIndex, int totalChunks, int dataOffset,
                        CancellationToken token)
            {
                  await Task.Factory.StartNew(async () =>
                              {
                                    if (chunkIndex == 0 && EditorUtility.DisplayDialog("File Transfer", $"Receive '{fileName}'?", "Yes", "No"))
                                    {
                                          string savePath = EditorUtility.SaveFilePanel("Save File", "", fileName, Path.GetExtension(fileName)?.TrimStart('.'));

                                          if (!string.IsNullOrEmpty(savePath))
                                          {
                                                _receivingFileStream = new MemoryStream();
                                                _receivingFileName = savePath;
                                                AddLog($"Receiving file: {fileName}", MessageType.Notification);
                                          }
                                    }

                                    if (_receivingFileStream != null)
                                    {
                                          _receivingFileStream.Write(buffer, dataOffset, bytesRead - dataOffset);
                                          OnFileTransferProgress?.Invoke($"Receiving {fileName}", (float)(chunkIndex + 1) / totalChunks);

                                          if (chunkIndex == totalChunks - 1)
                                          {
                                                await File.WriteAllBytesAsync(_receivingFileName, _receivingFileStream.ToArray(), token);
                                                AddLog($"File saved to {_receivingFileName}", MessageType.Notification);
                                                _receivingFileStream.Dispose();
                                                _receivingFileStream = null;
                                                OnFileTransferProgress?.Invoke("", -1); // Clear
                                          }
                                    }
                              },
                              token,
                              TaskCreationOptions.None,
                              TaskScheduler.FromCurrentSynchronizationContext());
            }

            public void Disconnect()
            {
                  if (!IsConnected)
                  {
                        return;
                  }

                  IsConnected = false;
                  _cancellationTokenSource?.Cancel();
                  _networkStream?.Close();
                  _client?.Close();
                  _listener?.Stop();

                  _cancellationTokenSource?.Dispose();

                  AddLog("Disconnected.", MessageType.Log);
                  OnConnectionStatusChanged?.Invoke(false);
            }

            private void HandleException(string context, Exception e)
            {
                  AddLog($"{context}: {e.Message}", MessageType.Error);
                  Debug.LogError($"{context}: {e}");
                  Disconnect();
            }

            private void AddMessage(string author, string content, Color color)
            {
                  Messages.Add(new ChatMessage(content, MessageType.UserMessage, color, author));
                  OnMessageReceived?.Invoke(Messages[^1]);
            }

            private void AddLog(string log, MessageType type)
            {
                  Color color = type == MessageType.Error ? Color.red : ChatPreferences.LogMessageColor;
                  Messages.Add(new ChatMessage(log, type, color));
                  OnMessageReceived?.Invoke(Messages[^1]);
            }

            public void ClearChat() => Messages.Clear();
      }
}