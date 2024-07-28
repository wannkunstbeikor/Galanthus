using System;
using System.CommandLine;
using System.IO;
using Galanthus;
using StreamUtils;

namespace GalanthusCli;

internal static class Program
{
    private static int Main(string[] args)
    {
        RootCommand command = new("CLI app to load Games made with the Snowdrop engine.");

        Argument<DirectoryInfo> gameArgument = new("game-path", "The path to the games Directory.");
        Option<string?> platformOption = new("--platform", "The platform identifier, if not specified pc is going to be used.");
        Option<FileInfo?> keyOption = new("--key", "The path to a key file.");
        Option<FileInfo?> ivOption = new("--iv", "The path to a iv file.");

        Command loadCommand = new("load", "Load game")
        {
            gameArgument,
            platformOption,
            keyOption,
            ivOption
        };

        loadCommand.SetHandler(LoadGame, gameArgument, platformOption, keyOption, ivOption);

        command.AddCommand(loadCommand);

        return command.InvokeAsync(args).Result;
    }

    private static void LoadGame(DirectoryInfo inGameDirectory, string? inPlatform, FileInfo? inKeyFileInfo, FileInfo? inIvFileInfo)
    {
        if (!inGameDirectory.Exists)
        {
            Console.WriteLine($"Game does not exist at {inGameDirectory.FullName}");
            return;
        }

        Platform platform = Platform.Pc;
        if (!string.IsNullOrEmpty(inPlatform))
        {
            if (!Enum.TryParse(inPlatform, true, out platform))
            {
                Console.WriteLine("Failed to parse platform, has to be one of the following:");
                foreach (string variable in Enum.GetNames(typeof(Platform)))
                {
                    Console.WriteLine($"{variable}");
                }
            }
        }

        if (inKeyFileInfo is not null && inKeyFileInfo.Exists)
        {
            using Stream stream = inKeyFileInfo.OpenRead();
            KeyManager.Key = new Block<byte>((int)inKeyFileInfo.Length);
            stream.ReadExactly(KeyManager.Key);
        }

        if (inIvFileInfo is not null && inIvFileInfo.Exists)
        {
            using Stream stream = inIvFileInfo.OpenRead();
            KeyManager.Iv = new Block<byte>((int)inIvFileInfo.Length);
            stream.ReadExactly(KeyManager.Iv);
        }

        if (!GameManager.LoadGame(inGameDirectory.FullName, platform))
        {
            Console.WriteLine("Failed to load game.");
        }

        KeyManager.Key?.Dispose();
        KeyManager.Iv?.Dispose();
    }
}