using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// A data structure that contains a message's data, 
    /// but does not capture its unique identifier.
    /// </summary>
    public sealed class MessageData
    {
        public MessageData(
            int SenderId, int RecipientId, 
            string Message)
        {
            this.SenderId = SenderId;
            this.RecipientId = RecipientId;
            this.Message = Message;
        }

        /// <summary>
        /// Gets the sender identifier.
        /// </summary>
        /// <value>The sender identifier.</value>
        [JsonProperty("senderId")]
        public int SenderId { get; private set; }

        /// <summary>
        /// Gets the recipient identifier.
        /// </summary>
        /// <value>The recipient identifier.</value>
        [JsonProperty("recipientId")]
        public int RecipientId { get; private set; }

        /// <summary>
        /// Gets the message's contents.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; private set; }
    }
}

