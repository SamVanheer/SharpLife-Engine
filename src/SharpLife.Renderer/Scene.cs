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

using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Input;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace SharpLife.Renderer
{
    public class Scene : IViewState
    {
        private const int LightScaleRange = 256;

        private readonly List<IResourceContainer> _resourceContainers = new List<IResourceContainer>();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();
        private readonly List<IRenderable> _renderables = new List<IRenderable>();

        private readonly IVariable _mainGamma;

        private readonly IVariable _textureGamma;

        private readonly IVariable _lightingGamma;

        private readonly IVariable _brightness;

        private readonly IVariable _overbright;

        private bool _lightingSettingChanged;

        public Camera Camera { get; }

        public Vector3 Origin => Camera.Position;

        public Vector3 Angles => VectorUtils.VectorToAngles(Vector3.Transform(Camera.DefaultLookDirection, Camera.RotationMatrix));

        private DirectionalVectors _viewAngles;

        public DirectionalVectors ViewVectors => _viewAngles;

        public Scene(IInputSystem inputSystem, ICommandContext commandContext, GraphicsDevice gd, int viewWidth, int viewHeight)
        {
            Camera = new Camera(inputSystem, gd, viewWidth, viewHeight);
            _updateables.Add(Camera);

            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            //TODO: might need to move these somewhere else
            //TODO: archived, but circular dependency on engine helpers makes it impossible for now
            _mainGamma = commandContext.RegisterVariable(
                new VariableInfo("mat_gamma")
                .WithValue(2.5f)
                .WithHelpInfo("The main gamma value to use for gamma correction")
                .WithMinMaxFilter(1.8f, 3.0f)
                .WithChangeHandler((ref VariableChangeEvent _) => _lightingSettingChanged = true));

            _textureGamma = commandContext.RegisterVariable(
                new VariableInfo("mat_texgamma")
                .WithValue(2.0f)
                .WithHelpInfo("The texture gamma value to use for gamma correction")
                .WithMinMaxFilter(1.8f, null)
                .WithChangeHandler((ref VariableChangeEvent _) => _lightingSettingChanged = true));

            _lightingGamma = commandContext.RegisterVariable(
                new VariableInfo("mat_lightgamma")
                .WithValue(2.5f)
                .WithHelpInfo("The lighting gamma value to use for gamma correction")
                .WithMinMaxFilter(1.8f, null)
                .WithChangeHandler((ref VariableChangeEvent _) => _lightingSettingChanged = true));

            //TODO: archived
            _brightness = commandContext.RegisterVariable(
                new VariableInfo("mat_brightness")
                .WithValue(0.0f)
                .WithHelpInfo("The lighting brightness multiplier. Set to 0 to disable")
                .WithMinMaxFilter(0.0f, 2.0f)
                .WithChangeHandler((ref VariableChangeEvent _) => _lightingSettingChanged = true));

            //TODO: archived
            _overbright = commandContext.RegisterVariable(
                new VariableInfo("mat_overbright")
                .WithValue(1)
                .WithHelpInfo("Enable or disable overbright lighting")
                .WithBooleanFilter()
                .WithChangeHandler((ref VariableChangeEvent _) => _lightingSettingChanged = true));
        }

        public void AddContainer(IResourceContainer r)
        {
            _resourceContainers.Add(r);
        }

        public void RemoveContainer(IResourceContainer r)
        {
            _resourceContainers.Remove(r);
        }

        public void AddRenderable(IRenderable r)
        {
            _renderables.Add(r);
        }

        public void RemoveRenderable(IRenderable r)
        {
            _renderables.Remove(r);
        }

        public void AddUpdateable(IUpdateable updateable)
        {
            Debug.Assert(updateable != null);
            _updateables.Add(updateable);
        }

        public void Update(float deltaSeconds)
        {
            VectorUtils.AngleToVectors(Angles, out _viewAngles);

            foreach (IUpdateable updateable in _updateables)
            {
                updateable.Update(deltaSeconds);
            }
        }

        public void RenderAllStages(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            RenderAllSingleThread(gd, cl, sc);
        }

        private void RenderAllSingleThread(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            CheckLightingInfo(gd, sc);

            float depthClear = gd.IsDepthRangeZeroToOne ? 0f : 1f;

            // Main scene
            cl.SetFramebuffer(sc.MainSceneFramebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Grey);
            var fbWidth = sc.MainSceneFramebuffer.Width;
            var fbHeight = sc.MainSceneFramebuffer.Height;
            cl.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            cl.SetFullViewports();
            cl.SetFullScissorRects();
            cl.ClearDepthStencil(depthClear);
            sc.UpdateCameraBuffers(cl);
            var cameraFrustum = new BoundingFrustum(Camera.ViewMatrix * Camera.ProjectionMatrix);
            Render(gd, cl, sc, RenderPasses.Standard, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.AlphaBlend, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);
            Render(gd, cl, sc, RenderPasses.Overlay, cameraFrustum, Camera.Position, _renderQueues[0], _renderableStage[0], null, false);

            if (sc.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(sc.MainSceneColorTexture, sc.MainSceneResolvedColorTexture);
            }

            //Render to the swap chain buffer
            cl.SetFramebuffer(gd.SwapchainFramebuffer);
            fbWidth = gd.SwapchainFramebuffer.Width;
            fbHeight = gd.SwapchainFramebuffer.Height;
            cl.SetFullViewports();
            Render(gd, cl, sc, RenderPasses.SwapchainOutput, new BoundingFrustum(), Camera.Position, _renderQueues[0], _renderableStage[0], null, false);

            cl.End();

            gd.SubmitCommands(cl);
        }

        public void Render(
            GraphicsDevice gd,
            CommandList rc,
            SceneContext sc,
            RenderPasses pass,
            BoundingFrustum frustum,
            Vector3 viewPosition,
            RenderQueue renderQueue,
            List<IRenderable> renderableList,
            Comparer<RenderItemIndex> comparer,
            bool threaded)
        {
            renderQueue.Clear();

            renderableList.Clear();
            CollectFreeObjects(pass, renderableList);
            renderQueue.AddRange(renderableList, viewPosition);

            if (comparer == null)
            {
                renderQueue.Sort();
            }
            else
            {
                renderQueue.Sort(comparer);
            }

            foreach (var renderable in renderQueue)
            {
                renderable.Render(gd, rc, sc, pass);
            }
        }

        private void UpdateLightingInfo(GraphicsDevice gd, SceneContext sc)
        {
            if (sc.LightingInfoBuffer != null)
            {
                int lightScale;

                if (_overbright.Boolean)
                {
                    lightScale = LightScaleRange;
                }
                else
                {
                    //Round up the scale
                    lightScale = (int)((Math.Pow(2.0, 1.0 / _lightingGamma.Float) * LightScaleRange) + 0.5);
                }

                var info = new LightingInfo
                {
                    MainGamma = _mainGamma.Float,
                    TextureGamma = _textureGamma.Float,
                    LightingGamma = _lightingGamma.Float,
                    Brightness = _brightness.Float,
                    LightScale = lightScale,
                    OverbrightEnabled = _overbright.Boolean,
                };

                gd.UpdateBuffer(sc.LightingInfoBuffer, 0, ref info);

                _lightingSettingChanged = false;
            }
        }

        private void CheckLightingInfo(GraphicsDevice gd, SceneContext sc)
        {
            if (_lightingSettingChanged)
            {
                UpdateLightingInfo(gd, sc);
            }
        }

        private readonly RenderQueue[] _renderQueues = Enumerable.Range(0, 4).Select(_ => new RenderQueue()).ToArray();
        private readonly List<IRenderable>[] _renderableStage = Enumerable.Range(0, 4).Select(_ => new List<IRenderable>()).ToArray();

        private void CollectFreeObjects(RenderPasses renderPass, List<IRenderable> renderables)
        {
            foreach (var r in _renderables)
            {
                if ((r.RenderPasses & renderPass) != 0)
                {
                    renderables.Add(r);
                }
            }
        }

        public void DestroyAllDeviceObjects(ResourceScope scope)
        {
            foreach (var r in _resourceContainers)
            {
                r.DestroyDeviceObjects(scope);
            }
        }

        public void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc, ResourceScope scope)
        {
            UpdateLightingInfo(gd, sc);

            foreach (var r in _resourceContainers)
            {
                r.CreateDeviceObjects(gd, cl, sc, scope);
            }
        }

        private class RenderPassesComparer : IEqualityComparer<RenderPasses>
        {
            public bool Equals(RenderPasses x, RenderPasses y) => x == y;
            public int GetHashCode(RenderPasses obj) => ((byte)obj).GetHashCode();
        }
    }
}
