using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LorlinaBot.Discord.Models;

namespace LorlinaBot.Discord
{
    /// <summary>
    /// Client managing the interaction with Discord.
    /// </summary>
    public class DiscordClient : IDisposable
    {
        private const string CHANNELTYPE_TEXT = "text channel";
        private const string CHANNELTYPE_VOICE = "voice channel";

        private DiscordSocketClient _client;

        private ClientConfiguration _configuration;

        private bool _isStarted;

        /// <summary>
        /// Gets all configured server text channels as dictionary with channel names as keys and channel ids as values.
        /// </summary>
        public Dictionary<string, ulong> TextChannels { get; private set; }

        /// <summary>
        /// Gets all configured server voices channels as dictionary with channel names as keys and channel ids as values.
        /// </summary>
        public Dictionary<string, ulong> VoiceChannels { get; private set; }

        #region Singleton
        #pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields
        private static readonly Lazy<DiscordClient> _instance = new Lazy<DiscordClient>(() => new DiscordClient());

        /// <summary>
        /// Gets the instance of the DiscordClient class.
        /// </summary>
        public static DiscordClient Instance
        {
            get { return _instance.Value; }
        }

        private DiscordClient()
        {
            this._client = new DiscordSocketClient();

            this._client.Log += this.LogAsync;
            this._client.Ready += this.ReadyAsync;
            this._client.MessageReceived += this.MessageReceivedAsync;
            this._client.ChannelCreated += this.ChannelCreatedAsync;
            this._client.ChannelUpdated += this.ChannelUpdateAsync;
            this._client.ChannelDestroyed += this.ChannelDestroyedAsync;

            this._isStarted = false;
        }
        #pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
        #endregion Singleton

        #region Disposable
        #pragma warning disable SA1514 // Element documentation header must be preceded by blank line
        /// <summary>
        /// Releases all resources used by the current instance of the DiscordClient class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the DiscordClient and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._isStarted)
                {
                    this.StopClientAsync().GetAwaiter().GetResult();
                }

                this._client.Log -= this.LogAsync;
                this._client.Ready -= this.ReadyAsync;
                this._client.MessageReceived -= this.MessageReceivedAsync;
                this._client.ChannelCreated -= this.ChannelCreatedAsync;
                this._client.ChannelUpdated -= this.ChannelUpdateAsync;
                this._client.ChannelDestroyed -= this.ChannelDestroyedAsync;

                this._client.Dispose();
            }
        }
        #pragma warning restore SA1514 // Element documentation header must be preceded by blank line
        #endregion Disposable

        /// <summary>
        /// Connect the instance of the DiscordClient class to the correct Discord bot.
        /// </summary>
        /// <param name="clientToken">Token of the bot you want to associated the DiscordClient instance to.</param>
        /// <param name="configuration">Configuration of the client.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartClientAsync(string clientToken, ClientConfiguration configuration)
        {
            if (!this._isStarted)
            {
                await this._client.LoginAsync(TokenType.Bot, clientToken).ConfigureAwait(false);
                await this._client.StartAsync().ConfigureAwait(false);

                this._configuration = configuration;
                this.TextChannels = new Dictionary<string, ulong>();
                this.VoiceChannels = new Dictionary<string, ulong>();

                this._isStarted = true;
            }
        }

        /// <summary>
        /// Disconnect the instance of the DiscordClient.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StopClientAsync()
        {
            if (this._isStarted)
            {
                await this._client.StopAsync().ConfigureAwait(false);
                await this._client.LogoutAsync().ConfigureAwait(false);

                this._isStarted = false;
            }
        }

        /// <summary>
        /// Sends a specific message to the given channel.
        /// </summary>
        /// <param name="channelId">Id of the channel on which you want to send message.</param>
        /// <param name="message">Message to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendMessageAsync(ulong channelId, string message)
        {
            var channel = this._client.GetChannel(channelId);

            if (channel.GetType() == typeof(SocketTextChannel))
            {
                var textChannel = (SocketTextChannel)channel;

                await textChannel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }

        #region EventFunctions
        private async Task LogAsync(LogMessage log)
        {
            await Task.Run(() => Console.WriteLine($"#LOG: {log.ToString()}")).ConfigureAwait(false);
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"#INFO: Bot {this._client.CurrentUser} is connected.");

            var server = this._client.GetGuild(this._configuration.ConfiguredServerId);
            if (server == null)
            {
                Console.WriteLine("#WARNING: didn't found configured server.");
                return;
            }

            this.TextChannels = server.TextChannels
                .GroupBy(tc => tc.Name)
                .ToDictionary(tc => tc.Key, tc => tc.First().Id);
            Console.WriteLine($"#INFO: {this.TextChannels.Count} text channels found on connection.");

            this.VoiceChannels = server.VoiceChannels
                .GroupBy(tc => tc.Name)
                .ToDictionary(tc => tc.Key, tc => tc.First().Id);
            Console.WriteLine($"#INFO: {this.VoiceChannels.Count} voice channels found on connection.");

            if (this._configuration.ClientConnectedMessage != null
                && this._configuration.NotificationChannelName != null
                && this.TextChannels.ContainsKey(this._configuration.NotificationChannelName))
            {
                await this.SendMessageAsync(this.TextChannels[this._configuration.NotificationChannelName], this._configuration.ClientConnectedMessage).ConfigureAwait(false);
            }
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == this._client.CurrentUser.Id)
            {
                return;
            }

            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("pong!").ConfigureAwait(false);
            }
        }

        private async Task ChannelCreatedAsync(SocketChannel newChannel)
        {
            if (newChannel.GetType() == typeof(SocketTextChannel))
            {
                var newTextChannel = (SocketTextChannel)newChannel;
                await Task.Run(() => this.AddChannelToDictionary(newTextChannel.Name, newTextChannel.Id, this.TextChannels, CHANNELTYPE_TEXT)).ConfigureAwait(false);
            }
            else if (newChannel.GetType() == typeof(SocketVoiceChannel))
            {
                var newVoiceChannel = (SocketVoiceChannel)newChannel;
                await Task.Run(() => this.AddChannelToDictionary(newVoiceChannel.Name, newVoiceChannel.Id, this.VoiceChannels, CHANNELTYPE_VOICE)).ConfigureAwait(false);
            }
        }

        private async Task ChannelUpdateAsync(SocketChannel outdatedChannel, SocketChannel updatedChannel)
        {
            string outdatedChannelName = null;
            string updatedChannelName = null;

            if (outdatedChannel.GetType() == typeof(SocketTextChannel))
            {
                var outdatedTextChannel = (SocketTextChannel)outdatedChannel;
                var updatedTextChannel = (SocketTextChannel)updatedChannel;
                outdatedChannelName = outdatedTextChannel.Name;
                updatedChannelName = updatedTextChannel.Name;
            }
            else if (outdatedChannel.GetType() == typeof(SocketVoiceChannel))
            {
                var outdatedVoiceChannel = (SocketVoiceChannel)outdatedChannel;
                var updatedVoiceChannel = (SocketVoiceChannel)updatedChannel;
                outdatedChannelName = outdatedVoiceChannel.Name;
                updatedChannelName = updatedVoiceChannel.Name;
            }
            else
            {
                return;
            }

            if (outdatedChannelName != updatedChannelName)
            {
                await this.ChannelDestroyedAsync(outdatedChannel).ConfigureAwait(false);
                await this.ChannelCreatedAsync(updatedChannel).ConfigureAwait(false);
            }
        }

        private async Task ChannelDestroyedAsync(SocketChannel destroyedChannel)
        {
            if (destroyedChannel.GetType() == typeof(SocketTextChannel))
            {
                var destroyedTextChannel = (SocketTextChannel)destroyedChannel;
                await Task.Run(() => this.RemoveChannelFromDictionary(destroyedTextChannel.Name, destroyedTextChannel.Id, this.TextChannels, CHANNELTYPE_TEXT)).ConfigureAwait(false);
            }
            else if (destroyedChannel.GetType() == typeof(SocketVoiceChannel))
            {
                var destroyedVoiceChannel = (SocketVoiceChannel)destroyedChannel;
                await Task.Run(() => this.RemoveChannelFromDictionary(destroyedVoiceChannel.Name, destroyedVoiceChannel.Id, this.VoiceChannels, CHANNELTYPE_VOICE)).ConfigureAwait(false);
            }
        }
        #endregion EventFunctions

        #region ChannelsManager
        private void AddChannelToDictionary(string channelName, ulong channelId, Dictionary<string, ulong> dictionary, string channelType)
        {
            if (dictionary.ContainsKey(channelName))
            {
                Console.WriteLine($"#WARNING: {channelType} \"{channelName}\" already exists, you won't be able to use bot function by using its name.");
            }
            else
            {
                dictionary.Add(channelName, channelId);
                Console.WriteLine($"#INFO: \"{channelName}\" added to {channelType} list.");
            }
        }

        private void RemoveChannelFromDictionary(string channelName, ulong channelId, Dictionary<string, ulong> dictionary, string channelType)
        {
            if (dictionary.ContainsKey(channelName)
                    && dictionary[channelName] == channelId)
            {
                dictionary.Remove(channelName);
                Console.WriteLine($"#INFO: \"{channelName}\" removed from {channelType} list.");
            }
        }
        #endregion ChannelsManager
    }
}
