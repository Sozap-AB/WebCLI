using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebCli.Internal;

namespace WebCli
{
    public static class AppBuilderExtensions
    {
        public static void UseWebCli(this WebApplication app, WebCliOptions options)
        {
            var controller = new WebCliController(options, app.Services);

            app.UseWebSockets();

            app.MapGet(options.Path, controller.Index);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == $"{options.Path}/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await controller.HandleWebSocket(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }
            });
        }
    }
}
