using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UniChat.Editor.Core;
using UniChat.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace UniChat.Editor.UI
{
      public sealed class ChatEditorWindow : EditorWindow
      {
            private ChatService _chatService;
            private Vector2 _scrollPosition;
            private string _chatInput = "";
            private string _ipAddress = "127.0.0.1";
            private readonly StringBuilder _chatLogBuilder = new();

            [MenuItem("Tools/UniChat")]
            public static void ShowWindow() => GetWindow<ChatEditorWindow>("UniChat");

            private void OnEnable()
            {
                  ChatPreferences.Load();
                  _chatService = new ChatService();
                  SubscribeToEvents();
                  RebuildChatLog();
            }

            private void OnDisable()
            {
                  UnsubscribeFromEvents();
                  _chatService?.Disconnect();
                  ChatPreferences.Save();
            }

            private void OnGUI()
            {
                  DrawConnectionPanel();
                  DrawChatPanel();
                  DrawInputPanel();
            }

            private void DrawConnectionPanel()
            {
                  EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                  string status = _chatService.IsConnected ? "Connected" : "Disconnected";
                  Color statusColor = _chatService.IsConnected ? Color.green : Color.red;

                  GUILayout.Label(new GUIContent($" Status: {status}", EditorGUIUtility.IconContent("d_winbtn_mac_max@2x").image),
                              new GUIStyle(EditorStyles.label) { normal = { textColor = statusColor }, fontStyle = FontStyle.Bold });

                  GUILayout.FlexibleSpace();

                  if (_chatService.IsConnected)
                  {
                        if (GUILayout.Button("Disconnect", EditorStyles.toolbarButton))
                        {
                              _chatService.Disconnect();
                        }
                  }
                  else
                  {
                        if (GUILayout.Button("Start Server", EditorStyles.toolbarButton))
                        {
                              _ = _chatService.StartServerAsync();
                        }

                        _ipAddress = GUILayout.TextField(_ipAddress, EditorStyles.toolbarTextField, GUILayout.Width(120));

                        if (GUILayout.Button("Connect", EditorStyles.toolbarButton))
                        {
                              _ = _chatService.ConnectToServerAsync(_ipAddress);
                        }
                  }

                  EditorGUILayout.EndHorizontal();
            }

            private void DrawChatPanel()
            {
                  _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                  var chatStyle = new GUIStyle(EditorStyles.textArea) { richText = true, wordWrap = true };
                  EditorGUILayout.SelectableLabel(_chatLogBuilder.ToString(), chatStyle, GUILayout.ExpandHeight(true));
                  EditorGUILayout.EndScrollView();
            }

            private void DrawInputPanel()
            {
                  EditorGUILayout.BeginHorizontal();

                  ChatPreferences.Username = EditorGUILayout.TextField(ChatPreferences.Username, GUILayout.Width(100));
                  _chatInput = EditorGUILayout.TextField(_chatInput);

                  bool enterPressed = Event.current.isKey && Event.current.keyCode == KeyCode.Return && _chatInput.Length > 0;

                  if ((GUILayout.Button("Send", GUILayout.Width(60)) || enterPressed) && _chatService.IsConnected)
                  {
                        _ = _chatService.SendMessageAsync(_chatInput);
                        _chatInput = "";
                        GUI.FocusControl(null);
                  }

                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.BeginHorizontal();

                  if (GUILayout.Button("Send File", GUILayout.Height(25)))
                  {
                        string path = EditorUtility.OpenFilePanel("Select file to send", "", "");

                        if (!string.IsNullOrEmpty(path))
                        {
                              _ = _chatService.SendFileAsync(path);
                        }
                  }

                  if (GUILayout.Button("Show My IP", GUILayout.Height(25)))
                  {
                        ShowLocalIPAddress();
                  }

                  if (GUILayout.Button("Clear Chat", GUILayout.Height(25)))
                  {
                        _chatService.ClearChat();
                        RebuildChatLog();
                  }

                  if (GUILayout.Button("Options", GUILayout.Height(25)))
                  {
                        ChatOptionsWindow.ShowWindow();
                  }

                  EditorGUILayout.EndHorizontal();
            }

#region Event Handling

            private void SubscribeToEvents()
            {
                  _chatService.OnMessageReceived += HandleMessageReceived;
                  _chatService.OnFileTransferProgress += HandleFileProgress;
                  EditorApplication.update += Repaint;
            }

            private void UnsubscribeFromEvents()
            {
                  _chatService.OnMessageReceived -= HandleMessageReceived;
                  _chatService.OnFileTransferProgress -= HandleFileProgress;
                  EditorApplication.update -= Repaint;
            }

            private void HandleMessageReceived(ChatMessage message)
            {
                  _chatLogBuilder.AppendLine(message.GetFormattedMessage());
                  _scrollPosition.y = float.MaxValue;
            }

            private static void HandleFileProgress(string activity, float progress)
            {
                  if (progress < 0)
                  {
                        EditorUtility.ClearProgressBar();
                  }
                  else
                  {
                        EditorUtility.DisplayProgressBar("File Transfer", activity, progress);
                  }
            }

            private void RebuildChatLog()
            {
                  _chatLogBuilder.Clear();

                  foreach (ChatMessage msg in _chatService.Messages)
                  {
                        _chatLogBuilder.AppendLine(msg.GetFormattedMessage());
                  }
            }

#endregion

            private static void ShowLocalIPAddress()
            {
                  string localIP = "Not available";

                  try
                  {
                        foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(static ip => ip.AddressFamily == AddressFamily.InterNetwork))
                        {
                              localIP = ip.ToString();

                              break;
                        }
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error getting local IP: {e.Message}");
                  }

                  EditorUtility.DisplayDialog("Local IP Address", "Your IP Address is: " + localIP, "OK");
            }
      }
}