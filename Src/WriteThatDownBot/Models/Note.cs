// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;

namespace WriteThatDownBot.Models
{
    public class Note
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = NoteType.Private;

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        /// <summary>
        /// Gets or sets the message body in markdown.
        /// </summary>
        /// <remarks>
        /// Teams returns most messages in HTML, we use a helper library to convert the html to markdown
        /// and store it here so we can display the content in cards.
        /// The original value for this field as returned by teams is stored in <see cref="MessageActionsPayload"/>.
        /// </remarks>
        [JsonProperty("noteBody")]
        public string NoteBody { get; set; } = "";

        [JsonProperty("messageLinkUrl")]
        public string MessageLinkUrl { get; set; }

        [JsonProperty("messageActionsPayload")]
        public MessageActionsPayload MessageActionsPayload { get; set; }

        /// <summary>
        /// Helper to get the icon to show for the note.
        /// </summary>
        public string GetNoteIconUrl()
        {
            var imageName = "sharednote.png";
            if (Type == NoteType.Private)
            {
                imageName = "privatenote.png?42";
            }

            return $"https://raw.githubusercontent.com/gabog/RequestResponseBotGateway/master/{imageName}";
        }
    }
}