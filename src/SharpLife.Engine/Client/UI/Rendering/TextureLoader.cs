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
using SharpLife.Engine.Client.UI.Rendering.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Veldrid;
using Veldrid.ImageSharp;

namespace SharpLife.Engine.Client.UI.Rendering
{
    public class TextureLoader
    {
        //Different from ImageConversionUtils.MinimumMaxTextureSize because that is used to create non-zero sizes
        //This ensures the maximum size is constrained to a sensible value
        private const int MinimumMaxSize = 128;
        private const int DefaultMaxSize = 256;
        private const int DefaultRoundDown = 3;
        private const int DefaultPicMip = 0;
        private const bool DefaultRescaleTextures = true;

        private readonly IVariable<uint> _maxSize;

        private readonly IVariable<uint> _roundDown;

        private readonly IVariable<uint> _picMip;

        private readonly IVariable<bool> _powerOf2Textures;

        public TextureLoader(ICommandContext commandContext)
        {
            if (commandContext == null)
            {
                throw new ArgumentNullException(nameof(commandContext));
            }

            _maxSize = commandContext.RegisterVariable(
                new VirtualVariableInfo<uint>("mat_max_size", DefaultMaxSize)
                .WithHelpInfo("This is used to constrain texture sizes")
                .ConfigureFilters(filters => filters.WithMinMaxFilter(MinimumMaxSize, null, true)));

            _roundDown = commandContext.RegisterVariable(
                new VirtualVariableInfo<uint>("mat_round_down", DefaultRoundDown)
                .WithHelpInfo("If not 0, this is used to round down texture sizes when rescaling to power of 2. Ignored when mat_powerof2textures is false")
                .ConfigureFilters(filters => filters.WithMinMaxFilter(ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, true)));

            _picMip = commandContext.RegisterVariable(
                new VirtualVariableInfo<uint>("mat_picmip", DefaultPicMip)
                .WithHelpInfo("If not 0, this is the number of times to halve the size of texture sizes")
                .ConfigureFilters(filters => filters.WithMinMaxFilter(ImageConversionUtils.MinSizeExponent, ImageConversionUtils.MaxSizeExponent, true)));

            _powerOf2Textures = commandContext.RegisterVariable(
                new VirtualVariableInfo<bool>("mat_powerof2textures", DefaultRescaleTextures)
                .WithHelpInfo("Whether to rescale textures to power of 2"));
        }

        /// <summary>
        /// Computes the scaled size of a texture that has the given width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public (uint, uint) ComputeScaledSize(uint width, uint height)
        {
            return ImageConversionUtils.ComputeScaledSize(width, height, _powerOf2Textures.Value, _maxSize.Value, _roundDown.Value, _picMip.Value);
        }

        private Image<Rgba32> InternalConvertTexture(IndexedColor256Image inputImage, TextureFormat textureFormat)
        {
            var pixels = ImageConversionUtils.ConvertIndexedToRgba32(inputImage, textureFormat);

            //Alpha tested textures have their fully transparent pixels modified so samplers won't sample the color used and blend it
            //This stops the color from bleeding through
            if (textureFormat == TextureFormat.AlphaTest
                || textureFormat == TextureFormat.IndexAlpha)
            {
                ImageConversionUtils.BoxFilter3x3(pixels, inputImage.Width, inputImage.Height);
            }

            //Rescale image to nearest power of 2
            (var scaledWidth, var scaledHeight) = ComputeScaledSize((uint)inputImage.Width, (uint)inputImage.Height);

            var scaledPixels = ImageConversionUtils.ResampleImage(new Span<Rgba32>(pixels), inputImage.Width, inputImage.Height, (int)scaledWidth, (int)scaledHeight);

            return Image.LoadPixelData(scaledPixels, (int)scaledWidth, (int)scaledHeight);
        }

        /// <summary>
        /// Converts an indexed 256 color image to Rgba32
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="textureFormat"></param>
        /// <returns></returns>
        public Image<Rgba32> ConvertTexture(IndexedColor256Image inputImage, TextureFormat textureFormat)
        {
            if (inputImage == null)
            {
                throw new ArgumentNullException(nameof(inputImage));
            }

            return InternalConvertTexture(inputImage, textureFormat);
        }

        private ImageSharpTexture InternalLoadTexture(IndexedColor256Image inputImage, TextureFormat textureFormat, bool mipmap)
        {
            var image = InternalConvertTexture(inputImage, textureFormat);

            return new ImageSharpTexture(image, mipmap);
        }

        /// <summary>
        /// Loads a texture and creates it using the specified factory
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="textureFormat"></param>
        /// <param name="mipmap"></param>
        /// <param name="gd"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public Texture LoadTexture(IndexedColor256Image inputImage, TextureFormat textureFormat, bool mipmap, GraphicsDevice gd, ResourceFactory factory)
        {
            if (inputImage == null)
            {
                throw new ArgumentNullException(nameof(inputImage));
            }

            var imageSharpTexture = InternalLoadTexture(inputImage, textureFormat, mipmap);

            return imageSharpTexture.CreateDeviceTexture(gd, factory);
        }

        /// <summary>
        /// Loads a texture and creates it using the specified graphics device's resource factory
        /// The texture is added to the given cache
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="textureFormat"></param>
        /// <param name="mipmap"></param>
        /// <param name="name"></param>
        /// <param name="gd"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public Texture LoadTexture(IndexedColor256Image inputImage, TextureFormat textureFormat, bool mipmap, string name, GraphicsDevice gd, ResourceCache cache)
        {
            if (inputImage == null)
            {
                throw new ArgumentNullException(nameof(inputImage));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var imageSharpTexture = InternalLoadTexture(inputImage, textureFormat, mipmap);

            return cache.AddTexture2D(gd, gd.ResourceFactory, imageSharpTexture, name);
        }
    }
}
