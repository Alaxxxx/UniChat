using System;
using UniChat.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace UniChat.Editor.UI
{
      public sealed class ChatOptionsWindow : EditorWindow
      {
            private Color _userColor;
            private Color _logColor;
            private int _chunkSize;

            private readonly string[] _chunkSizeOptions = { "8KB", "64KB", "256KB", "512KB" };
            private readonly int[] _chunkSizeValues = { 8192, 65536, 262144, 524288 };
            private int _selectedChunkIndex;

            public static void ShowWindow()
            {
                  var window = GetWindow<ChatOptionsWindow>("Chat Options");
                  window.minSize = new Vector2(300, 120);
                  window.maxSize = new Vector2(300, 120);
            }

            private void OnEnable()
            {
                  _userColor = ChatPreferences.UserMessageColor;
                  _logColor = ChatPreferences.LogMessageColor;
                  _chunkSize = ChatPreferences.ChunkSize;
                  _selectedChunkIndex = Array.IndexOf(_chunkSizeValues, _chunkSize);

                  if (_selectedChunkIndex == -1)
                  {
                        _selectedChunkIndex = 1;
                  }
            }

            private void OnGUI()
            {
                  EditorGUILayout.LabelField("Chat Appearance", EditorStyles.boldLabel);
                  _userColor = EditorGUILayout.ColorField("User Message Color", _userColor);
                  _logColor = EditorGUILayout.ColorField("Log Message Color", _logColor);

                  EditorGUILayout.Space();

                  EditorGUILayout.LabelField("Network Settings", EditorStyles.boldLabel);
                  _selectedChunkIndex = EditorGUILayout.Popup("File Chunk Size", _selectedChunkIndex, _chunkSizeOptions);
                  _chunkSize = _chunkSizeValues[_selectedChunkIndex];

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Apply and Close"))
                  {
                        ApplySettings();
                        Close();
                  }
            }

            private void ApplySettings()
            {
                  ChatPreferences.UserMessageColor = _userColor;
                  ChatPreferences.LogMessageColor = _logColor;
                  ChatPreferences.ChunkSize = _chunkSize;
                  ChatPreferences.Save();
            }
      }
}