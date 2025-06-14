using UnityEngine;

namespace UniChat.Editor.Data
{
      public sealed class ChatMessage
      {
            public string Author { get; }
            public string Content { get; }
            public MessageType Type { get; }
            public Color DisplayColor { get; }

            public ChatMessage(string content, MessageType type, Color color, string author = "System")
            {
                  Author = author;
                  Content = content;
                  Type = type;
                  DisplayColor = color;
            }

            public string GetFormattedMessage()
            {
                  string colorHex = ColorUtility.ToHtmlStringRGB(DisplayColor);

                  return $"<color=#{colorHex}><b>{Author}:</b> {Content}</color>";
            }
      }
}