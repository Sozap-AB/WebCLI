using WebCli;
using WebCLI.Examples.Commands;

var builder = WebApplication.CreateBuilder(args);

Type[] commands = [
    typeof(HelloWorldCommand),
    typeof(AsyncOperationCommand),
    typeof(AsyncMultistepOperationCommand),
    typeof(ProgressBarCommand),
    typeof(InterruptableCommand),
    typeof(InterruptableActiveCommand),
];

foreach (var command in commands)
    builder.Services.AddTransient(command);

var app = builder.Build();

app.UseWebCli(new WebCliOptions(
    commands, // all command types should be passed here
    greetings: "\nWelcome message! Can be whatever you want it to be!\nEnter \"help\" to see options.\n"
));

await app.RunAsync();
