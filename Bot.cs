using System;
using BepInEx.Configuration;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChaosMod
{

    class Bot
    {
        TwitchClient client;
        Plugin plugin;

        public Bot(Plugin plugin)
        {
            this.plugin = plugin;
            ConnectionCredentials credentials = new ConnectionCredentials(plugin.configUsername.Value, plugin.configAccessToken.Value);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, plugin.configUsername.Value);

            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;

            client.Connect();
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected!");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if(e.ChatMessage.Message.StartsWith("1") || e.ChatMessage.Message.StartsWith("2") || e.ChatMessage.Message.StartsWith("3") || e.ChatMessage.Message.StartsWith("4") || e.ChatMessage.Message.StartsWith("5") || e.ChatMessage.Message.StartsWith("6") || e.ChatMessage.Message.StartsWith("7") || e.ChatMessage.Message.StartsWith("8")){
                plugin.voteCast(int.Parse(e.ChatMessage.Message[0].ToString()), e.ChatMessage.Username);
            }
        }
    }
}
