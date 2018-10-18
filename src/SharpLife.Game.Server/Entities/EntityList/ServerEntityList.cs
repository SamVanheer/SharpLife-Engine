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

using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Engine.Shared.API.Engine.Shared;
using SharpLife.Game.Server.API;
using SharpLife.Game.Shared.Entities.EntityList;
using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Game.Shared.Maps;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using SharpLife.Utility;
using System;

namespace SharpLife.Game.Server.Entities.EntityList
{
    public sealed class ServerEntityList : BaseEntityList<BaseEntity>
    {
        private readonly INetworkObjectList _entitiesNetworkList;

        private readonly EntityContext _entityContext;

        public ServerEntityList(
            EntityDictionary entityDictionary,
            int maxClients,
            INetworkObjectList entitiesNetworkList,
            IServerEngine serverEngine,
            ITime time,
            IEngineModels engineModels,
            IMapInfo mapInfo,
            GameServer gameServer,
            ServerEntities entities)
            : base(entityDictionary, maxClients)
        {
            _entitiesNetworkList = entitiesNetworkList ?? throw new ArgumentNullException(nameof(entitiesNetworkList));
            _entityContext = new EntityContext(serverEngine, time, engineModels, mapInfo, gameServer, entities, this);
        }

        protected override void OnEntityCreated(EntityEntry entry)
        {
            entry.Entity.Context = _entityContext;

            if (entry.Entity.Networked)
            {
                var networkObject = _entitiesNetworkList.CreateNetworkObject(entry.Entity);

                entry.Entity.NetworkObject = networkObject;
            }
        }

        protected override void OnEntityDestroyed(EntityEntry entry)
        {
            if (entry.Entity.Networked)
            {
                _entitiesNetworkList.DestroyNetworkObject(entry.Entity.NetworkObject);
            }
        }
    }
}
