﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes a message generated by e.g. an alarms &amp; events system that also contains a cursor position.
    /// </summary>
    public sealed class EventMessageWithCursorPosition : EventMessageBase {

        /// <summary>
        /// The cursor position for the event message.
        /// </summary>
        public string CursorPosition { get; }


        /// <summary>
        /// Creates a new <see cref="EventMessage"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the event message.
        /// </param>
        /// <param name="utcEventTime">
        ///   The UTC timestamp of the event.
        /// </param>
        /// <param name="priority">
        ///   The event priority.
        /// </param>
        /// <param name="category">
        ///   The event category.
        /// </param>
        /// <param name="message">
        ///   The event message.
        /// </param>
        /// <param name="properties">
        ///   Additional event properties.
        /// </param>
        /// <param name="cursorPosition">
        ///   The cursor position for the event message.
        /// </param>
        public EventMessageWithCursorPosition(string id, DateTime utcEventTime, EventPriority priority, string category, string message, IDictionary<string, string> properties, string cursorPosition)
            : base(id, utcEventTime, priority, category, message, properties) {
            CursorPosition = cursorPosition;
        }

    }

}