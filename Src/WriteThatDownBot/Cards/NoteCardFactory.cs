// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using WriteThatDownBot.Models;

namespace WriteThatDownBot.Cards
{
    /// <summary>
    /// A static factory to create note cards.
    /// </summary>
    public static class NoteCardFactory
    {
        public static Attachment CreateNoteListAttachment(List<Note> notes)
        {
            var cardResourcePath = "WriteThatDownBot.Cards.NoteListTemplate.json";

            using (var stream = typeof(NoteCardFactory).Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var cardJson = reader.ReadToEnd();
                    var cardTemplate = new AdaptiveCardTemplate(cardJson);
                    var notesJson = $"{{\"notes\": {JsonConvert.SerializeObject(notes)}}}";
                    var adaptiveCard = JsonConvert.DeserializeObject<AdaptiveCard>(cardTemplate.Expand(notesJson));
                    return new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = adaptiveCard
                    };
                }
            }
        }

        /// <summary>
        /// Creates an adaptive card for the given template and bind the object data to it.
        /// </summary>
        /// <param name="templateName">The adaptive card template.</param>
        /// <param name="data">The object containing the data to bind to the card.</param>
        /// <returns></returns>
        public static AdaptiveCard GetAdaptiveCard(string templateName, object data)
        {
            var cardResourcePath = "WriteThatDownBot.Cards." + templateName;

            using (var stream = typeof(NoteCardFactory).Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var cardJson = reader.ReadToEnd();
                    var cardTemplate = new AdaptiveCardTemplate(cardJson);
                    return JsonConvert.DeserializeObject<AdaptiveCard>(cardTemplate.Expand(data));
                }
            }
        }
    }
}