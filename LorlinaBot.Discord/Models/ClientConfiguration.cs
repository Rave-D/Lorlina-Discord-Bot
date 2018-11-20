namespace LorlinaBot.Discord.Models
{
    /// <summary>
    /// Configuration of the discord client.
    /// </summary>
    public class ClientConfiguration
    {
        /// <summary>
        /// Gets or sets the id of the server with which the client should interact.
        /// </summary>
        public ulong ConfiguredServerId { get; set; }

        /// <summary>
        /// Gets or sets the message that will be send on client connection.
        /// </summary>
        public string ClientConnectedMessage { get; set; }

        /// <summary>
        /// Gets or sets the name of the channel on which the notification messages will be send.
        /// </summary>
        public string NotificationChannelName { get; set; }
    }
}
