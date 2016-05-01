using Newtonsoft.Json;
using System;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// A data structure that contains a message's data, 
    /// but does not capture its unique identifier.
    /// </summary>
    public sealed class MessageData
    {
		[JsonConstructor]
		private MessageData()
		{ }

        public MessageData(
            Guid SenderGuid, Guid RecipientGuid, 
            string Message)
        {
            this.SenderGuid = SenderGuid;
            this.RecipientGuid = RecipientGuid;
            this.Message = Message;
        }

        /// <summary>
        /// Gets the sender's GUID string.
        /// </summary>
        [JsonProperty("senderId", Required = Required.Always)]
        public string SenderGuidString
        {
            get { return SenderGuid.ToString(); }
            private set { SenderGuid = new Guid(value); }
        }

        /// <summary>
        /// Gets the sender identifier.
        /// </summary>
        /// <value>The sender identifier.</value>
        [JsonIgnore]
        public Guid SenderGuid { get; private set; }

        /// <summary>
        /// Gets the recipient's GUID string.
        /// </summary>
        [JsonProperty("recipientId", Required = Required.Always)]
        public string RecipientGuidString
        {
            get { return RecipientGuid.ToString(); }
            private set { RecipientGuid = new Guid(value); }
        }

        /// <summary>
        /// Gets the recipient identifier.
        /// </summary>
        /// <value>The recipient identifier.</value>
        [JsonIgnore]
        public Guid RecipientGuid { get; private set; }

        /// <summary>
        /// Gets the message's contents.
        /// </summary>
        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; private set; }
    }
}

