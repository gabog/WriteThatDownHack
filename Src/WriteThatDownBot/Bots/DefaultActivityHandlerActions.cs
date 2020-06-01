// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReverseMarkdown;
using WriteThatDownBot.Models;
using WriteThatDownBot.Utilities;

namespace WriteThatDownBot.Bots
{
    public partial class DefaultActivityHandler<T>
    {
        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            switch (action.CommandId)
            {
                // These commandIds are defined in the Teams App Manifest.
                case TeamsCommands.TakeQuickNote:
                    var quickNoteCard = GetAdaptiveCard("NewNoteTemplate.json", new Note());

                    return CreateActionResponse("Create quick note", quickNoteCard);

                case TeamsCommands.NoteFromMessage:
                    var converter = new Converter();
                    var newNote = new Note
                    {
                        Title = FixString(new string(HtmlUtilities.ConvertToPlainText(action.MessagePayload.Body.Content).Take(42).ToArray())),
                        NoteBody = FixString(converter.Convert(action.MessagePayload.Body.Content)),
                    };
                    var newNoteCard = GetAdaptiveCard("NewNoteTemplate.json", newNote);

                    return CreateActionResponse("Create note from message", newNoteCard);

                default:
                    throw new NotImplementedException($"Invalid Fetch CommandId: {action.CommandId}");
            }
        }

        /// <summary>
        ///  Helper to remove empty lines and escape double quotes.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private string FixString(string original)
        {
            var noLines = Regex.Replace(original, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            return noLines.Replace("\"", "\\\"");
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            switch (action.CommandId)
            {
                // These commandIds are defined in the Teams App Manifest.
                case TeamsCommands.TakeQuickNote:
                    var quickNote = ObjectPath.MapValueTo<Note>(action.Data);
                    quickNote.Id = Guid.NewGuid().ToString();
                    quickNote.MessageActionsPayload = new MessageActionsPayload(from: new MessageActionsPayloadFrom(new MessageActionsPayloadUser(displayName: turnContext.Activity.From.Name)), createdDateTime: DateTime.Now.ToString(CultureInfo.InvariantCulture));

                    // Save the note.
                    await _notesService.AddNoteAsync(quickNote);

                    return new MessagingExtensionActionResponse();

                case TeamsCommands.NoteFromMessage:
                    var newNote = ObjectPath.MapValueTo<Note>(action.Data);
                    newNote.Id = Guid.NewGuid().ToString();
                    newNote.MessageActionsPayload = action.MessagePayload;
                    // BUG: action.MessagePayload doesn't to have the linkToMessage, so we manually pull it from the value property of the activity.
                    newNote.MessageLinkUrl = JObject.FromObject(turnContext.Activity.Value)["messagePayload"]?["linkToMessage"]?.ToString();

                    // Save the note.
                    await _notesService.AddNoteAsync(newNote);

                    return new MessagingExtensionActionResponse();

                default:
                    throw new NotImplementedException($"Invalid CommandId: {action.CommandId}");
            }
        }

        private static Task<MessagingExtensionActionResponse> CreateActionResponse(string title, AdaptiveCard newNoteCard)
        {
            return Task.FromResult(new MessagingExtensionActionResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo
                    {
                        Title = title,
                        Height = 400,
                        Width = 500,
                        Card = new Attachment
                        {
                            Content = newNoteCard,
                            ContentType = AdaptiveCard.ContentType,
                        },
                    },
                },
            });
        }

        /// <summary>
        /// Creates an adaptive card for the given template and bind the object data to it.
        /// </summary>
        /// <param name="templateName">The adaptive card template.</param>
        /// <param name="data">The object containing the data to bind to the card.</param>
        /// <returns></returns>
        private AdaptiveCard GetAdaptiveCard(string templateName, object data)
        {
            var cardResourcePath = "WriteThatDownBot.Cards." + templateName;

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var cardText = reader.ReadToEnd();
                    var cardTemplate = new AdaptiveCardTemplate(cardText);
                    return JsonConvert.DeserializeObject<AdaptiveCard>(cardTemplate.Expand(data));
                }
            }
        }
    }
}