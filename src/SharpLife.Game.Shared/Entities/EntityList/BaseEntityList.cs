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

using SharpLife.Game.Shared.Entities.MetaData;
using SharpLife.Networking.Shared.Communication.NetworkObjectLists;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpLife.Game.Shared.Entities.EntityList
{
    /// <summary>
    /// Base class for lists of entities deriving from <typeparamref name="TBaseEntity"/>
    /// </summary>
    /// <typeparam name="TBaseEntity"></typeparam>
    public abstract class BaseEntityList<TBaseEntity>
        where TBaseEntity : class, IEntity
    {
        public const ushort InvalidId = ushort.MaxValue;

        /// <summary>
        /// Maximum number of entities that can be created at any one time
        /// If you need more than this, change the following:
        /// <see cref="MaxSupportedEntities"/>
        /// <see cref="InvalidId"/>
        /// <see cref="EntityHandle"/>
        /// <see cref="EntityEntry"/>
        /// </summary>
        public const ushort MaxSupportedEntities = ushort.MaxValue - 1;

        public const int NoHighestIndex = -1;

        protected sealed class EntityEntry
        {
            public ushort serialNumber = 1;

            public TBaseEntity entity;

            public bool Valid => entity != null;

            public EntityEntry()
            {
            }
        }

        private readonly EntityDictionary _entityDictionary;

        private readonly List<EntityEntry> _entities = new List<EntityEntry>();

        /// <summary>
        /// The number of entities that are currently in existence
        /// </summary>
        public int EntityCount { get; private set; }

        /// <summary>
        /// The highest entity index currently in use
        /// If no entities exist, this is <see cref="NoHighestIndex"/>
        /// </summary>
        public int HighestIndex { get; private set; } = NoHighestIndex;

        //TODO: need to know maxplayers value to reserve entries
        protected BaseEntityList(EntityDictionary entityDictionary)
        {
            _entityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));
        }

        public TBaseEntity GetEntity(in ObjectHandle handle)
        {
            //Handle is default constructed
            if (handle.Id == InvalidId)
            {
                return null;
            }

            //Handle was created with a specific id that is beyond the valid range, but not the invalid id
            if (handle.Id >= _entities.Count)
            {
                throw new ArgumentException("The given handle has an invalid id", nameof(handle));
            }

            var entry = _entities[handle.Id];

            if (entry.serialNumber == handle.SerialNumber)
            {
                return entry.entity;
            }

            return null;
        }

        private ObjectHandle InternalGetNextEntity(int startIndex)
        {
            for (var i = startIndex; i <= HighestIndex; ++i)
            {
                if (_entities[i].Valid == true)
                {
                    return new ObjectHandle(i, _entities[i].serialNumber);
                }
            }

            return default;
        }

        /// <summary>
        /// Gets a handle to the first entity in the list, or a default handle if no entities exist
        /// </summary>
        /// <returns></returns>
        public ObjectHandle GetFirstEntity()
        {
            return InternalGetNextEntity(0);
        }

        /// <summary>
        /// Gets a handle to the next entity in the list after the entity represented by the given handle, or a default handle if there are no more entities in the list
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public ObjectHandle GetNextEntity(in ObjectHandle handle)
        {
            if (!handle.Valid)
            {
                throw new ArgumentException(nameof(handle));
            }

            return InternalGetNextEntity(handle.Id + 1);
        }

        private void AllocateEntry()
        {
            _entities.Add(new EntityEntry());
        }

        private int FindFreeEntry()
        {
            for (int i = 0; i < _entities.Count; ++i)
            {
                if (_entities[i].entity == null)
                {
                    return i;
                }
            }

            if (_entities.Count >= ObjectHandle.MaxSupportedObjects)
            {
                throw new NoFreeEntityEntriesException();
            }

            //Allocate a new entry for this index
            AllocateEntry();

            return _entities.Count - 1;
        }

        private void EnsureCapacity(int index)
        {
            if (_entities.Count <= index)
            {
                for (var i = _entities.Count; i <= index; ++i)
                {
                    AllocateEntry();
                }
            }
        }

        protected void InternalAddEntityToList(in ObjectHandle handle, TBaseEntity instance)
        {
            //Need to resize to account for networked entities whose index falls outside our current range
            EnsureCapacity(handle.Id);

            var entry = _entities[handle.Id];

            if (entry.Valid)
            {
                throw new InvalidOperationException($"Entity index {handle.Id} already has an entity associated with it");
            }

            entry.entity = instance;
            entry.serialNumber = (ushort)handle.SerialNumber;

            //It is not an error to not set the classname here, the caller is responsible for it

            OnEntityCreated(entry);

            ++EntityCount;

            if (handle.Id > HighestIndex)
            {
                HighestIndex = handle.Id;
            }
        }

        /// <summary>
        /// Creates an entity and adds it at the specified index
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="EntityIndexOutOfRangeException">If the given index is out of range</exception>
        /// <exception cref="InvalidOperationException">If there is already an entity at the given index</exception>
        /// <exception cref="ArgumentException">If the type does not have a parameterless constructor</exception>
        public TBaseEntity CreateEntity(EntityDictionary.EntityMetaData metaData, int index)
        {
            if (metaData == null)
            {
                throw new ArgumentNullException(nameof(metaData));
            }

            if (index < 0 || index >= ObjectHandle.MaxSupportedObjects)
            {
                throw new EntityIndexOutOfRangeException(index, ObjectHandle.MaxSupportedObjects);
            }

            EnsureCapacity(index);

            try
            {
                var instance = (IEntity)Activator.CreateInstance(metaData.Type);

                var entry = _entities[index];

                if (entry.Valid)
                {
                    throw new InvalidOperationException($"Entity index {index} already has an entity associated with it");
                }

                entry.entity = (TBaseEntity)instance;
                //The serial number is not incremented on creation because no valid handles can exist for it at this time
                //++entry.serialNumber;

                instance.Handle = new ObjectHandle(index, entry.serialNumber);

                instance.ClassName = metaData.Name;

                OnEntityCreated(entry);

                ++EntityCount;

                if (index > HighestIndex)
                {
                    HighestIndex = index;
                }

                return entry.entity;
            }
            catch (MissingMethodException e)
            {
                //This is already handled in EntityDictionary, but in the event that it somehow doesn't get caught there (e.g. custom metadata) it'll still be handled
                //This shouldn't be a EntityInstantiationException because it's a programmer error and should be immediately visible
                throw new InvalidOperationException($"Entity class \"{metaData.Name}\" does not have a parameterless constructor", e);
            }
        }

        public TBaseEntity CreateEntity(EntityDictionary.EntityMetaData metaData)
        {
            return CreateEntity(metaData, FindFreeEntry());
        }

        /// <summary>
        /// Creates a new entity by class name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NoSuchEntityClassException">If no entity class by the given name exists</exception>
        public TBaseEntity CreateEntityByName(string name)
        {
            return CreateEntity(_entityDictionary.FindEntityMetaData(name) ?? throw new NoSuchEntityClassException(name));
        }

        protected abstract void OnEntityCreated(EntityEntry entry);

        private void InternalDestroyEntity(int id, bool updateHighestIndex)
        {
            var entry = _entities[id];

            OnEntityDestroyed(entry);

            entry.entity = null;
            ++entry.serialNumber;

            --EntityCount;

            if (updateHighestIndex && HighestIndex == id)
            {
                int i;

                for (i = id - 1; i >= 0; --i)
                {
                    if (_entities[i].entity != null)
                    {
                        break;
                    }
                }

                //Will either be a valid index, or -1
                HighestIndex = i;
            }
        }

        public void DestroyEntity(TBaseEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var handle = entity.Handle;

            if (handle.Id >= _entities.Count)
            {
                throw new ArgumentException("Entity has an invalid handle", nameof(entity));
            }

            Debug.Assert(ReferenceEquals(entity, _entities[handle.Id].entity), "Entity must match the entry reference");

            InternalDestroyEntity(handle.Id, true);
        }

        protected abstract void OnEntityDestroyed(EntityEntry entry);

        /// <summary>
        /// Destroys all entities
        /// </summary>
        public void Clear()
        {
            //Anything past the highest index won't be valid anyway
            for (var i = 0; i <= HighestIndex; ++i)
            {
                if (_entities[i].entity != null)
                {
                    InternalDestroyEntity(i, false);
                }
            }

            HighestIndex = NoHighestIndex;
        }
    }
}
