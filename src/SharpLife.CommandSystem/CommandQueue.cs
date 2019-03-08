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
using SharpLife.CommandSystem.Commands;
using System;
using System.Collections.Generic;

namespace SharpLife.CommandSystem
{
    internal sealed class CommandQueue : ICommandQueue
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Commands that have been queued up for execution
        /// </summary>
        private readonly List<ICommandArgs> _commandsToExecute = new List<ICommandArgs>();

        public int QueuedCommandCount => _commandsToExecute.Count;

        public bool Wait { get; set; }

        public CommandQueue(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private bool CheckValidContext(CommandContext context)
        {
            if (context._destroyed)
            {
                _logger.Information("Attempted to queue commands for destroyed context {ContextName}", context.Name);
                return false;
            }

            return true;
        }

        public void QueueCommands(ICommandContext context, string commandText)
        {
            InternalQueueCommands((CommandContext)context, commandText);
        }

        internal void InternalQueueCommands(CommandContext context, string commandText)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (!CheckValidContext(context))
            {
                return;
            }

            var commands = CommandUtils.ParseCommands(context, commandText);

            commands.ForEach(_commandsToExecute.Add);
        }

        public void InsertCommands(ICommandContext context, string commandText, int index = 0)
        {
            InternalInsertCommands((CommandContext)context, commandText, index);
        }

        internal void InternalInsertCommands(CommandContext context, string commandText, int index = 0)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (index < 0 || index > _commandsToExecute.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!CheckValidContext(context))
            {
                return;
            }

            var commands = CommandUtils.ParseCommands(context, commandText);

            var startIndex = index;

            commands.ForEach(command => _commandsToExecute.Insert(startIndex++, command));
        }

        public void Execute()
        {
            while (!Wait && _commandsToExecute.Count > 0)
            {
                var commandArgs = _commandsToExecute[0];
                _commandsToExecute.RemoveAt(0);

                var command = commandArgs.Context.FindCommand<BaseCommand>(commandArgs.Name);

                if (command != null)
                {
                    command.OnCommand(commandArgs);
                }
                //This is different from the original; there aliases are checked before cvars
                else if (commandArgs.Context.Aliases.TryGetValue(commandArgs.Name, out var aliasedCommand))
                {
                    InsertCommands(commandArgs.Context, aliasedCommand);
                }
                else
                {
                    _logger.Information($"Could not find command {commandArgs.Name}");
                }
            }

            if (Wait)
            {
                Wait = false;
            }
        }
    }
}
