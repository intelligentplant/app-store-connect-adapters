﻿using System;
using System.Collections.Generic;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a message generated by e.g. an alarms &amp; events system.
    /// </summary>
    public sealed class EventMessage : EventMessageBase {

        /// <summary>
        /// Creates a new <see cref="EventMessage"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the event message. If <see langword="null"/>, an 
        ///   identifier will be generated.
        /// </param>
        /// <param name="topic">
        ///   The event message topic e.g. the MQTT channel that emitted the message.
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
        public EventMessage(
            string id, 
            string? topic,
            DateTime utcEventTime, 
            EventPriority priority, 
            string? category, 
            string? message, 
            IEnumerable<AdapterProperty>? properties
        ) : base(id ?? Guid.NewGuid().ToString(), topic, utcEventTime, priority, category, message, properties) { }


        /// <summary>
        /// Creates a new <see cref="EventMessage"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the event message. If <see langword="null"/>, an 
        ///   identifier will be generated.
        /// </param>
        /// <param name="topic">
        ///   The event message topic e.g. the MQTT channel that emitted the message.
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
        public static EventMessage Create(string id, string? topic, DateTime utcEventTime, EventPriority priority, string? category, string? message, IEnumerable<AdapterProperty>? properties) {
            return new EventMessage(id, topic, utcEventTime, priority, category, message, properties);
        }
    
    }

}
