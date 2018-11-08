using System;
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
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartClientAsync(string clientToken)
        {
            await this._client.LoginAsync(TokenType.Bot, clientToken).ConfigureAwait(false);
            await this._client.StartAsync().ConfigureAwait(false);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{this._client.CurrentUser} is connected!");

            return Task.CompletedTask;
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
    }
}
