using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace LorlinaBot.Discord
{
    /// <summary>
    /// Client managing the interaction with Discord.
    /// </summary>
    public class DiscordClient : IDisposable
    {
        private DiscordSocketClient _client;

        /// <summary>
        /// Gets id of the server with which the client should interact.
        /// </summary>
        public ulong ConfiguredServerId { get; private set; }

        /// <summary>
        /// Gets all configured server text channels as dictionary with channel names as keys and channel ids as values.
        /// </summary>
        public Dictionary<string, ulong> TextChannels { get; private set; }

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
                this._client.Log -= this.LogAsync;
                this._client.Ready -= this.ReadyAsync;
                this._client.MessageReceived -= this.MessageReceivedAsync;
                this._client.ChannelCreated -= this.ChannelCreatedAsync;
                this._client.ChannelUpdated -= this.ChannelUpdateAsync;
                this._client.ChannelDestroyed -= this.ChannelDestroyedAsync;

                this._client.StopAsync();
                this._client.Dispose();
            }
        }
        #pragma warning restore SA1514 // Element documentation header must be preceded by blank line
        #endregion Disposable

        /// <summary>
        /// Connect the instance of the DiscordClient class to the correct Discord bot.
        /// </summary>
        /// <param name="clientToken">Token of the bot you want to associated the DiscordClient instance to.</param>
        /// <param name="serverId">Id of the server with which the client should interact with.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartClientAsync(string clientToken, ulong serverId)
        {
            await this._client.LoginAsync(TokenType.Bot, clientToken).ConfigureAwait(false);
            await this._client.StartAsync().ConfigureAwait(false);

            this.ConfiguredServerId = serverId;
            this.TextChannels = new Dictionary<string, ulong>();
        }

        public async Task SendMessageAsync(ulong channelId, string message)
        {
        }

        #region EventFunctions
        private async Task LogAsync(LogMessage log)
        {
            await Task.Run(() => Console.WriteLine($"#LOG: {log.ToString()}")).ConfigureAwait(false);
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"#INFO: Bot {this._client.CurrentUser} is connected.");

            var server = this._client.GetGuild(this.ConfiguredServerId);
            if (server == null)
            {
                Console.WriteLine("#WARNING: didn't found configured server.");
                return;
            }

            this.TextChannels = server.TextChannels
                .GroupBy(tc => tc.Name)
                .ToDictionary(tc => tc.Key, tc => tc.First().Id);
            Console.WriteLine($"#INFO: {this.TextChannels.Count} text channels found on connection.");
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

                if (this.TextChannels.ContainsKey(newTextChannel.Name))
                {
                    Console.WriteLine($"#WARNING: text channel \"{newTextChannel.Name}\" already exists, you won't be able to use bot function by using its name.");
                }
                else
                {
                    await Task.Run(() => this.TextChannels.Add(newTextChannel.Name, newTextChannel.Id)).ConfigureAwait(false);
                    Console.WriteLine($"#INFO: \"{newTextChannel.Name}\" added to text channels list.");
                }
            }
        }

        private async Task ChannelUpdateAsync(SocketChannel outdatedChannel, SocketChannel updatedChannel)
        {
            if (outdatedChannel.GetType() == typeof(SocketTextChannel))
            {
                var outdatedTextChannel = (SocketTextChannel)outdatedChannel;
                var updatedTextChannel = (SocketTextChannel)updatedChannel;

                if (outdatedTextChannel.Name != updatedTextChannel.Name)
                {
                    await this.ChannelDestroyedAsync(outdatedChannel).ConfigureAwait(false);
                    await this.ChannelCreatedAsync(updatedChannel).ConfigureAwait(false);
                }
            }
        }

        private async Task ChannelDestroyedAsync(SocketChannel destroyedChannel)
        {
            if (destroyedChannel.GetType() == typeof(SocketTextChannel))
            {
                var destroyedTextChannel = (SocketTextChannel)destroyedChannel;

                if (this.TextChannels.ContainsKey(destroyedTextChannel.Name)
                    && this.TextChannels[destroyedTextChannel.Name] == destroyedTextChannel.Id)
                {
                    await Task.Run(() => this.TextChannels.Remove(destroyedTextChannel.Name)).ConfigureAwait(false);
                    Console.WriteLine($"#INFO: \"{destroyedTextChannel.Name}\" removed from text channels list.");
                }
            }
        }
        #endregion EventFunctions
    }
}
