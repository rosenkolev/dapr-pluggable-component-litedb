using System;
using System.IO;

using Dapr.PluggableComponents;

using DaprLiteDbComponent;

using LiteDbComponent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var socketDir = "/tmp/dapr-components-sockets";
var componentName = "litedb";
var socket = $"{socketDir}/{componentName}.sock";

Directory.CreateDirectory(socketDir);
if (File.Exists(socket)) // deleting socket in case of it already exists
{
    Console.WriteLine("Removing existing socket");
    File.Delete(socket);
}

var app = DaprPluggableComponentsApplication.Create();

app.Services.AddSingleton<MetadataStore>();

app.RegisterService(
    componentName,
    serviceBuilder =>
        serviceBuilder.RegisterStateStore(
            context =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<LiteDbStateStore>>();
                logger.LogInformation("Creating state store for instance '{0}' on socket '{1}'...", context.InstanceId, context.SocketPath);
                return new LiteDbStateStore(
                    context.InstanceId!,
                    logger,
                    context.ServiceProvider.GetRequiredService<MetadataStore>());
            }));

app.Run();
