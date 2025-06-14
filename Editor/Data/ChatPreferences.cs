using UnityEditor;
using UnityEngine;

namespace UniChat.Editor.Data
{
      public static class ChatPreferences
      {
            private const string UserMessageColorKey = "UniChat.UserMessageColor";
            private const string LogMessageColorKey = "UniChat.LogMessageColor";
            private const string ChunkSizeKey = "UniChat.ChunkSize";
            private const string UsernameKey = "UniChat.Username";

            public static Color UserMessageColor { get; set; } = Color.black;
            public static Color LogMessageColor { get; set; } = new(0, 0.3f, 0.7f);
            public static int ChunkSize { get; set; } = 65536;
            public static string Username { get; set; } = "User";

            public static void Load()
            {
                  Username = EditorPrefs.GetString(UsernameKey, "User");
                  ChunkSize = EditorPrefs.GetInt(ChunkSizeKey, 65536);

                  string userColorHtml = EditorPrefs.GetString(UserMessageColorKey, ColorUtility.ToHtmlStringRGB(Color.black));
                  ColorUtility.TryParseHtmlString("#" + userColorHtml, out Color userColor);
                  UserMessageColor = userColor;

                  string logColorHtml = EditorPrefs.GetString(LogMessageColorKey, ColorUtility.ToHtmlStringRGB(new Color(0, 0.3f, 0.7f)));
                  ColorUtility.TryParseHtmlString("#" + logColorHtml, out Color logColor);
                  LogMessageColor = logColor;
            }

            public static void Save()
            {
                  EditorPrefs.SetString(UsernameKey, Username);
                  EditorPrefs.SetInt(ChunkSizeKey, ChunkSize);
                  EditorPrefs.SetString(UserMessageColorKey, ColorUtility.ToHtmlStringRGB(UserMessageColor));
                  EditorPrefs.SetString(LogMessageColorKey, ColorUtility.ToHtmlStringRGB(LogMessageColor));
            }
      }
}