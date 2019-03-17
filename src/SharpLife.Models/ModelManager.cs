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

using SharpLife.FileSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SharpLife.Models
{
    public sealed class ModelManager : IModelManager
    {
        private readonly IReadOnlyList<IModelLoader> _modelLoaders;

        private readonly IFileSystem _fileSystem;

        private readonly Dictionary<string, IModel> _models;

        public IModel this[string modelName] => _models[modelName];

        public int Count => _models.Count;

        public IModel FallbackModel { get; private set; }

        public event Action<IModel> OnModelLoaded;

        public ModelManager(IFileSystem fileSystem, IReadOnlyList<IModelLoader> loaders)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _modelLoaders = loaders ?? throw new ArgumentNullException(nameof(loaders));

            //Names are case insensitive to account for differences in the filesystem
            _models = new Dictionary<string, IModel>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Contains(string modelName)
        {
            return _models.ContainsKey(modelName);
        }

        private void AddSubModel(string modelName, IModel model)
        {
            _models.Add(modelName, model);
        }

        internal IModel LoadModel(string modelName)
        {
            var reader = new BinaryReader(_fileSystem.OpenRead(modelName));

            foreach (var loader in _modelLoaders)
            {
                var model = loader.Load(modelName, _fileSystem, reader, AddSubModel, true);

                if (model != null)
                {
                    return model;
                }
            }

            return null;
        }

        private IModel InternalLoad(string modelName, bool throwOnFailure)
        {
            if (_models.TryGetValue(modelName, out var model))
            {
                return model;
            }

            model = LoadModel(modelName);

            if (model != null)
            {
                _models.Add(modelName, model);

                OnModelLoaded?.Invoke(model);
            }
            else
            {
                if (FallbackModel == null)
                {
                    if (throwOnFailure)
                    {
                        throw new InvalidOperationException($"Couldn't load model {modelName}; no fallback model loaded");
                    }

                    //Used by fallback model loading
                    return null;
                }

                model = FallbackModel;

                //Insert it anyway to avoid constant load attempts
                _models.Add(modelName, model);
            }

            return model;
        }

        public IModel Load(string modelName)
        {
            return InternalLoad(modelName, true);
        }

        public IModel LoadFallbackModel(string fallbackModelName)
        {
            FallbackModel = InternalLoad(fallbackModelName, false);

            //TODO: could construct a dummy model to use here
            if (FallbackModel == null)
            {
                throw new InvalidOperationException($"Couldn't load fallback model {fallbackModelName}");
            }

            return FallbackModel;
        }

        public void Clear()
        {
            _models.Clear();

            FallbackModel = null;
        }

        public IEnumerator<IModel> GetEnumerator()
        {
            return _models.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
