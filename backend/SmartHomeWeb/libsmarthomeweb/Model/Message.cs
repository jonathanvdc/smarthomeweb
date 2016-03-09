using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// A class that describes a message in the database.
    /// </summary>
    public sealed class Message : IEquatable<Message>
    {
        // Serialization demands a parameterless constructor.
        private Message()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartHomeWeb.Model.Message"/> class.
        /// </summary>
        /// <param name="Id">The message's unique identifier.</param>
        /// <param name="Data">A data structure that describes the message.</param>
        public Message(int Id, MessageData Data)
        { 
            this.Id = Id;
            this.Data = Data;
        }

        /// <summary>
        /// Gets the message's unique identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the message's data.
        /// </summary>
        [JsonProperty("data")]
        public MessageData Data { get; private set; }

        public bool Equals(Message Other)
        {
            return Id == Other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Message && Equals((Message)obj);
        }
    }
}
