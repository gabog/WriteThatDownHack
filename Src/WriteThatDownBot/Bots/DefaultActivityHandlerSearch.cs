// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;
using WriteThatDownBot.Cards;
using WriteThatDownBot.Models;
using WriteThatDownBot.Services;

namespace WriteThatDownBot.Bots
{
    // This partial class implements the search command functionality. 
    public partial class DefaultActivityHandler<T>
    {
        private readonly MockNoteService _notesService = new MockNoteService();

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

            // Search notes that match the criteria.
            var notes = await _notesService.FindAsync(text);

            // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            var attachments = notes.Select(note =>
            {
                var previewCard = new ThumbnailCard
                {
                    Title = note.Title,
                    Images = new List<CardImage> { new CardImage(GetNoteIconUrl(note), "Icon") },
                    Tap = new CardAction
                    {
                        Type = "invoke",
                        Value = note
                    }
                };

                var attachment = new MessagingExtensionAttachment
                {
                    ContentType = HeroCard.ContentType,
                    Content = new HeroCard { Title = note.Title },
                    Preview = previewCard.ToAttachment()
                    // Preview =  new Attachment(AdaptiveCard.ContentType, content: NoteCardFactory.GetAdaptiveCard("NoteTemplate.json", note))
                };

                return attachment;
            }).ToList();

            // The list of MessagingExtensionAttachments must we wrapped in a MessagingExtensionResult wrapped in a MessagingExtensionResponse.
            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = attachments
                }
            };
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {
            // The Preview card's Tap should have a Value property assigned, this will be returned to the bot in this event. 
            var selectedNote = query.ToObject<Note>();
            if (selectedNote == null)
            {
                throw new Exception("Unable to create a note from the selected item.");
            }

            var card = NoteCardFactory.GetAdaptiveCard("NoteTemplate.json", selectedNote);

            var fromMessage = (AdaptiveTextBlock)card.Body.Find(ae => ae.Id == "FromMessage");
            fromMessage.Text = $"{turnContext.Activity.From.Name} shared a note with you.";

            if (string.IsNullOrWhiteSpace(selectedNote.MessageLinkUrl))
            {
                // strip the action
                card.Actions.RemoveAt(0);
            }

            var imageElement = card.Body.FindAll(element => element.Id == "IconUrl").FirstOrDefault();

            var attachment = new Attachment
            {
                Content = card,
                ContentType = AdaptiveCard.ContentType,
            };

            var messageActivity = MessageFactory.Attachment(attachment).ApplyConversationReference(turnContext.Activity.GetConversationReference());
            await turnContext.SendActivityAsync(messageActivity, cancellationToken);

            // TODO maybe do this? https://stackoverflow.com/questions/57116935/how-to-use-adaptive-cards-on-teams-messaging-extension
            return new MessagingExtensionResponse();

            // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
            //var card = new ThumbnailCard
            //{
            //    //Title = selectedNote.Title,
            //    Images = new List<CardImage> { new CardImage(GetNoteIconUrl(selectedNote), "Icon") },
            //    //Subtitle = $"On {selectedNote.MessageActionsPayload.CreatedDateTime}, created by {selectedNote.MessageActionsPayload.From.Application.DisplayName}",
            //    Text = selectedNote.NoteBody,
            //    Buttons = string.IsNullOrWhiteSpace(selectedNote.MessageLinkUrl)
            //        ? null
            //        : new List<CardAction>
            //        {
            //            new CardAction
            //            {
            //                Type = ActionTypes.OpenUrl,
            //                Title = "Go to conversation",
            //                Value = selectedNote.MessageLinkUrl
            //            },
            //        }
            //};

            //var attachment = new MessagingExtensionAttachment
            //{
            //    ContentType = ThumbnailCard.ContentType,
            //    Content = card,
            //};

            //return new MessagingExtensionResponse
            //{
            //    ComposeExtension = new MessagingExtensionResult
            //    {
            //        Type = "result",
            //        AttachmentLayout = "list",
            //        Attachments = new List<MessagingExtensionAttachment> { attachment }
            //    }
            //};
        }

        private MessagingExtensionResponse NoteFromMessageCommand(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action)
        {
            // The user has chosen to share a message by choosing the 'Share Message' context menu command.
            var heroCard = new HeroCard
            {
                Title = $"{action.MessagePayload.From?.User?.DisplayName} originally sent this message:",
                Text = action.MessagePayload.Body.Content,
            };

            if (action.MessagePayload.Attachments != null && action.MessagePayload.Attachments.Count > 0)
            {
                // This sample does not add the MessagePayload Attachments.  This is left as an
                // exercise for the user.
                heroCard.Subtitle = $"({action.MessagePayload.Attachments.Count} Attachments not included)";
            }

            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment>
                    {
                        new MessagingExtensionAttachment
                        {
                            Content = heroCard,
                            ContentType = HeroCard.ContentType,
                            Preview = heroCard.ToAttachment(),
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Helper to get the icon to show for the note.
        /// </summary>
        public string GetNoteIconUrl(Note note)
        {
            var imageName = "sharednote.png";
            if (note.Type == NoteType.Private)
            {
                imageName = "privatenote.png?42";
            }

            return $"https://raw.githubusercontent.com/gabog/RequestResponseBotGateway/master/{imageName}";
        }
    }
}