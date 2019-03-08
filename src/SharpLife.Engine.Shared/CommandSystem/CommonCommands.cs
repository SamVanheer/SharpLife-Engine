﻿/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.FileSystem;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpLife.Engine.Shared.CommandSystem
{
    /// <summary>
    /// Utility functions to add common commands
    /// </summary>
    public static class CommonCommands
    {
        public static ICommand AddStuffCmds(this ICommandContext commandContext, ILogger logger, ICommandLine commandLine)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            return commandContext.RegisterCommand(new CommandInfo("stuffcmds", arguments =>
            {
                if (arguments.Count > 0)
                {
                    logger.Information("stuffcmds : execute command line parameters");
                    return;
                }

                var cmdIndex = 0;

                for (var i = 0; i < commandLine.Count - 1; ++i)
                {
                    var key = commandLine[i];

                    if (key.StartsWith("+"))
                    {
                        //Grab all arguments until the next key
                        var values = commandLine.GetValues(key);

                        commandContext.InsertCommands($"{key.Substring(1)} {string.Join(' ', values)}", cmdIndex++);

                        i += values.Count;
                    }
                }
            })
            .WithHelpInfo("Stuffs all command line arguments that contain commands into the command queue"));
        }

        /// <summary>
        /// Adds the exec command, to execute commands in cfg files
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="logger"></param>
        /// <param name="fileSystem"></param>
        /// <param name="gameConfigPathIDs">he game config path IDs to use for the exec command</param>
        /// <returns></returns>
        public static ICommand AddExec(this ICommandContext commandContext, ILogger logger, IFileSystem fileSystem, IReadOnlyList<string> gameConfigPathIDs)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (gameConfigPathIDs == null)
            {
                throw new ArgumentNullException(nameof(gameConfigPathIDs));
            }

            if (gameConfigPathIDs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one game config path ID", nameof(gameConfigPathIDs));
            }

            return commandContext.RegisterCommand(new CommandInfo("exec", arguments =>
            {
                if (arguments.Count < 1)
                {
                    logger.Information("exec <filename> : execute a script file");
                    return;
                }

                var fileName = arguments[0];

                if (fileName.IndexOfAny(new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar,
                    Path.VolumeSeparatorChar,
                    ':',
                    '~'
                }) != -1
                || fileName.Contains(".."))
                {
                    logger.Error($"exec {fileName}: invalid path.");
                    return;
                }

                if (fileName.IndexOf('.') != fileName.LastIndexOf('.'))
                {
                    logger.Error($"exec {fileName}: invalid filename.");
                    return;
                }

                var extension = Path.GetExtension(fileName);

                //TODO: need to define these extensions
                if (extension != ".cfg" && extension != ".rc")
                {
                    logger.Error($"exec {fileName}: not a .cfg or .rc file");
                    return;
                }

                var succeeded = false;

                foreach (var pathID in gameConfigPathIDs)
                {
                    if (fileSystem.Exists(fileName, pathID))
                    {
                        var text = fileSystem.ReadAllText(fileName, pathID);

                        logger.Debug($"execing {arguments[0]}");

                        commandContext.InsertCommands(text);

                        succeeded = true;
                        break;
                    }
                }

                if (!succeeded)
                {
                    logger.Error($"Couldn't exec {arguments[0]}");
                }
            })
            .WithHelpInfo("Executes a file containing commands"));
        }

        public static ICommand AddEcho(this ICommandContext commandContext, ILogger logger)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return commandContext.RegisterCommand(new CommandInfo("echo", arguments => logger.Information(arguments.ArgumentsString))
                .WithHelpInfo("Echoes the arguments to the console"));
        }

        public static ICommand AddAlias(this ICommandContext commandContext, ILogger logger)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return commandContext.RegisterCommand(new CommandInfo("alias", arguments =>
            {
                if (arguments.Count == 0)
                {
                    logger.Information("Current alias commands:");
                    foreach (var entry in commandContext.Aliases)
                    {
                        logger.Information($"{entry.Key}: {entry.Value}\n");
                    }
                    return;
                }

                // if the alias already exists, reuse it
                commandContext.SetAlias(arguments[0], arguments.ArgumentsAsString(1));
            })
            .WithHelpInfo("Aliases a command to a name"));
        }

        private sealed class FindCommands
        {
            private readonly ICommandContext _context;
            private readonly ILogger _logger;

            public FindCommands(ICommandContext context, ILogger logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void Find(string keyword, bool searchInHelpInfo = false, bool searchInValue = false)
            {
                var builder = new StringBuilder();

                builder.AppendLine("Find results:");

                var flags = FindCommandFlag.None;

                if (searchInHelpInfo)
                {
                    flags |= FindCommandFlag.SearchInHelpInfo;
                }

                if (searchInValue)
                {
                    flags |= FindCommandFlag.SearchInValue;
                }

                foreach (var command in _context.FindCommands(keyword, flags))
                {
                    command.WriteCommandInfo(builder);
                    builder.AppendLine();
                }

                _logger.Information(builder.ToString());
            }
        }

        public static ICommand AddFind(this ICommandContext commandContext, ILogger logger)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return commandContext.RegisterCommand(new ProxyCommandInfo<Action<string, bool, bool>>("find", new FindCommands(commandContext, logger).Find)
                .WithHelpInfo("Finds commands and variables"));
        }
    }
}
