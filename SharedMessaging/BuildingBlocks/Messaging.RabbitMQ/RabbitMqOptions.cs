﻿namespace SharedMessaging.BuildingBlocks.Messaging.RabbitMQ;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "app.exchange";
    public string ExchangeType { get; set; } = "direct";
    public string QueuePrefix { get; set; } = "app";
    public string? DeadLetterExchange { get; set; } // <- no default
}
