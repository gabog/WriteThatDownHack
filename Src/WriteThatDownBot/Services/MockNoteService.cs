// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema.Teams;
using WriteThatDownBot.Models;

namespace WriteThatDownBot.Services
{
    /// <summary>
    /// Mock class to store notes (temporary for testing using ugly static variables)
    /// </summary>
    public class MockNoteService
    {
        private static readonly IList<Note> _privateNotes = new List<Note>();
        private static readonly IList<Note> _sharedNotes = new List<Note>();

        static MockNoteService()
        {
            // Add some mock data so we can test with some pre existing notes.
            _privateNotes.Add(new Note
            {
                Id = "1",
                Type = NoteType.Private,
                Title = "Review my PRs before the end of the day",
                MessageLinkUrl = "",
                MessageActionsPayload = new MessageActionsPayload()
            });
            _privateNotes.Add(new Note
            {
                Id = "2",
                Type = NoteType.Private,
                Title = "Andrew is the best person to reach on Teams issues",
                MessageLinkUrl = "",
                MessageActionsPayload = new MessageActionsPayload()
            });

            _sharedNotes.Add(new Note
            {
                Id = "3",
                Type = NoteType.Shared,
                Title = "The right answer is always 42",
                MessageLinkUrl = "",
                MessageActionsPayload = new MessageActionsPayload()
            });
            _sharedNotes.Add(new Note
            {
                Id = "4",
                Type = NoteType.Shared,
                MessageLinkUrl = "",
                Title = "Improve Azure page design for cognitive services",
                MessageActionsPayload = new MessageActionsPayload()
            });
        }

        public Task<List<Note>> FindAsync(string query)
        {
            var result = new List<Note>();
            if (query == "*")
            {
                result.AddRange(_privateNotes);
                result.AddRange(_sharedNotes);
            }
            else
            {
                result.AddRange(_privateNotes.Where(note => note.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)));
                result.AddRange(_sharedNotes.Where(note => note.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)));
            }
            return Task.FromResult(result);
        }

        public Task AddNoteAsync(Note note)
        {
            switch (note.Type)
            {
                case NoteType.Private:
                    _privateNotes.Add(note);
                    break;
                case NoteType.Shared:
                    _sharedNotes.Add(note);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}