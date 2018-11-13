namespace LorlinaBot.Sandbox
{
    using System;
    using System.Threading.Tasks;
    using LorlinaBot.Discord;

    class Program
    {
        private const string DISCORDBOT_TOKEN = "INSERT TOKEN HERE";

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            await DiscordClient.Instance.StartClientAsync(DISCORDBOT_TOKEN).ConfigureAwait(false);

            // Block the program until it is closed.
            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}
