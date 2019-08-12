﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Base class describing a message generated by e.g. an alarms &amp; events system.
    /// </summary>
    public abstract class EventMessageBase {

        /// <summary>
        /// A unique identifier for the event.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The UTC timestamp of the event.
        /// </summary>
        public DateTime UtcEventTime { get; }

        /// <summary>
        /// The priority associated with the event.
        /// </summary>
        public EventPriority Priority { get; }

        /// <summary>
        /// The event category.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The event message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Additional event properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; private set; }


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
        protected EventMessageBase(string id, DateTime utcEventTime, EventPriority priority, string category, string message, IDictionary<string, string> properties) {
            Id = id ?? Guid.NewGuid().ToString();
            UtcEventTime = utcEventTime.ToUniversalTime();
            Priority = priority;
            Category = category;
            Message = message;
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }

}