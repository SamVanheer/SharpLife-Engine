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

using SharpLife.Utility.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpLife.Renderer.Utility
{
    /// <summary>
    /// Utilities to convert images to <see cref="Rgba32"/>
    /// </summary>
    public static class ImageConversionUtils
    {
        public const byte AlphaTestTransparentIndex = 255;
        public const byte IndexedAlphaColorIndex = 255;

        private const int ResampleRatio = 0x10000;

        public const int MinimumMaxImageSize = 1;

        public const uint MinSizeExponent = 0;
        public static readonly uint MaxSizeExponent = (uint)(8U * Marshal.SizeOf<int>()) - 1;

        /// <summary>
        /// Convert an indexed 256 color image to an Rgba32 image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertNormal(IndexedColor256Image image)
        {
            var size = image.Size;

            var pixels = new Rgba32[size];

            foreach (var i in Enumerable.Range(0, size))
            {
                image.Palette[image.Pixels[i]].ToRgba32(ref pixels[i]);
            }

            return pixels;
        }

        /// <summary>
        /// Alpha test: convert all indices to their color except index 255, which is converted as fully transparent
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertAlphaTest(IndexedColor256Image image)
        {
            var size = image.Size;

            var pixels = new Rgba32[size];

            foreach (var i in Enumerable.Range(0, size))
            {
                var index = image.Pixels[i];

                if (index != AlphaTestTransparentIndex)
                {
                    image.Palette[index].ToRgba32(ref pixels[i]);
                }
                else
                {
                    //RGB values default to 0 in array initializer
                    pixels[i].A = 0;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Indexed alpha: grayscale indexed 255 color image with single color gradient
        /// Transparency is determined by index value
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Rgba32[] ConvertIndexedAlpha(IndexedColor256Image image)
        {
            var size = image.Size;

            var pixels = new Rgba32[size];

            ref var color = ref image.Palette[IndexedAlphaColorIndex];

            foreach (var i in Enumerable.Range(0, size))
            {
                pixels[i].Rgb = color;
                pixels[i].A = image.Pixels[i];
            }

            return pixels;
        }

        public static Rgba32[] ConvertIndexedToRgba32(IndexedColor256Image image, TextureFormat format)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            switch (format)
            {
                case TextureFormat.Normal:
                    return ConvertNormal(image);
                case TextureFormat.AlphaTest:
                    return ConvertAlphaTest(image);
                case TextureFormat.IndexAlpha:
                    return ConvertIndexedAlpha(image);

                default: throw new ArgumentException("Invalid texture format", nameof(format));
            }
        }

        private static Rgba32 InternalBoxFilter3x3(Span<Rgba32> pixels, int w, int h, int x, int y)
        {
            var numPixelsSampled = 0;

            int r = 0, g = 0, b = 0;

            for (var xIndex = 0; xIndex < 3; ++xIndex)
            {
                for (var yIndex = 0; yIndex < 3; ++yIndex)
                {
                    var column = (xIndex - 1) + x;
                    var row = (yIndex - 1) + y;

                    if (column >= 0 && column < w
                        && row >= 0 && row < h)
                    {
                        ref var pPixel = ref pixels[column + (w * row)];

                        if (pPixel.A != 0)
                        {
                            r += pPixel.R;
                            g += pPixel.G;
                            b += pPixel.B;
                            ++numPixelsSampled;
                        }
                    }
                }
            }

            if (numPixelsSampled == 0)
            {
                numPixelsSampled = 1;
            }

            return new Rgba32(
                (byte)(r / numPixelsSampled),
                (byte)(g / numPixelsSampled),
                (byte)(b / numPixelsSampled),
                pixels[x + (w * y)].A
            );
        }

        /// <summary>
        /// Averages the pixels in a 3x3 box around a pixel and returns the new value
        /// The alpha value is left unmodified
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="result"></param>
        public static Rgba32 BoxFilter3x3(Span<Rgba32> pixels, int w, int h, int x, int y)
        {
            if (w <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(w));
            }

            if (h <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(h));
            }

            if (x < 0 || x >= w)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= h)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            if (pixels.Length != w * h)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given image bounds");
            }

            return InternalBoxFilter3x3(pixels, w, h, x, y);
        }

        /// <summary>
        /// Averages the pixels in a 3x3 box around a pixel for all pixels
        /// Only pixels with alpha 0 are considered
        /// The alpha value is left unmodified
        /// The span is modified in place
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void BoxFilter3x3(Span<Rgba32> pixels, int w, int h)
        {
            if (w <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(w));
            }

            if (h <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(h));
            }

            if (pixels.Length != w * h)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given image bounds");
            }

            for (var i = 0; i < pixels.Length; ++i)
            {
                if (pixels[i].A == 0)
                {
                    pixels[i] = InternalBoxFilter3x3(
                        pixels,
                        w, h,
                        i % w, i / w);
                }
            }
        }

        /// <summary>
        /// Rescales and resamples an image
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="inWidth"></param>
        /// <param name="inHeight"></param>
        /// <param name="outWidth"></param>
        /// <param name="outHeight"></param>
        /// <returns></returns>
        public static Rgba32[] ResampleImage(Span<Rgba32> pixels, int inWidth, int inHeight, int outWidth, int outHeight)
        {
            if (inWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inWidth));
            }

            if (inHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inHeight));
            }

            if (outWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outWidth));
            }

            if (outHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outHeight));
            }

            if (pixels.Length != inWidth * inHeight)
            {
                throw new ArgumentException("The given pixels span does not match the size of the given image bounds");
            }

            var p1 = new int[outWidth];
            var p2 = new int[outWidth];

            if (outWidth > 0)
            {
                var xScale = (inWidth * ResampleRatio) / outWidth;

                var row1Index = xScale / 4;
                var row2Index = 3 * (xScale / 4);

                for (int i = 0; i < outWidth; ++i)
                {
                    p1[i] = row1Index / ResampleRatio;
                    p2[i] = row2Index / ResampleRatio;

                    row1Index += xScale;
                    row2Index += xScale;
                }
            }

            var output = new Rgba32[outWidth * outHeight];

            var slice = new Span<Rgba32>(output);

            for (var i = 0; i < outHeight; ++i)
            {
                var inrow = pixels.Slice(inWidth * (int)((i + 0.25) * inHeight / (float)outHeight));
                var inrow2 = pixels.Slice(inWidth * (int)((i + 0.75) * inHeight / (float)outHeight));

                for (var j = 0; j < outWidth; ++j)
                {
                    var row1Index = p1[j];
                    var row2Index = p2[j];

                    slice[j].R = (byte)((inrow2[row1Index].R + inrow[row2Index].R + inrow[row1Index].R + inrow2[row2Index].R) / 4);
                    slice[j].G = (byte)((inrow2[row1Index].G + inrow[row2Index].G + inrow[row1Index].G + inrow2[row2Index].G) / 4);
                    slice[j].B = (byte)((inrow2[row1Index].B + inrow[row2Index].B + inrow[row1Index].B + inrow2[row2Index].B) / 4);
                    slice[j].A = (byte)((inrow2[row1Index].A + inrow[row2Index].A + inrow[row1Index].A + inrow2[row2Index].A) / 4);
                }

                slice = slice.Slice(outWidth);
            }

            return output;
        }

        private static uint ComputeRoundedDownValue(uint value, bool doPowerOf2Rescale, uint maxValue, uint roundDownExponent, uint divisorExponent)
        {
            uint scaledValue;

            if (doPowerOf2Rescale)
            {
                scaledValue = MathUtils.NearestUpperPowerOf2(value);

                if ((roundDownExponent > 0) && (value < scaledValue) && (roundDownExponent == 1 || ((scaledValue - value) > (scaledValue >> (int)roundDownExponent))))
                {
                    scaledValue /= 2;
                }
            }
            else
            {
                scaledValue = value;
            }

            scaledValue >>= (int)divisorExponent;

            //Make sure it's always valid
            return Math.Clamp(scaledValue, 1U, maxValue);
        }

        /// <summary>
        /// Computes the new size of an image
        /// </summary>
        /// <param name="inWidth">Original width</param>
        /// <param name="inHeight">Original height</param>
        /// <param name="doPowerOf2Rescale">Whether to rescale images to power of 2</param>
        /// <param name="maxValue">The maximum value that a scaled size can be</param>
        /// <param name="roundDownExponent">Exponent used to round down the scaled size</param>
        /// <param name="divisorExponent">Exponent used to divide the scaled size further</param>
        /// <returns></returns>
        public static (uint, uint) ComputeScaledSize(uint inWidth, uint inHeight, bool doPowerOf2Rescale, uint maxValue, uint roundDownExponent, uint divisorExponent)
        {
            if (inWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inWidth));
            }

            if (inHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inHeight));
            }

            if (maxValue < MinimumMaxImageSize)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            if (roundDownExponent < MinSizeExponent || roundDownExponent > MaxSizeExponent)
            {
                throw new ArgumentOutOfRangeException(nameof(roundDownExponent));
            }

            if (divisorExponent < MinSizeExponent || divisorExponent > MaxSizeExponent)
            {
                throw new ArgumentOutOfRangeException(nameof(divisorExponent));
            }

            //Rescale image to nearest power of 2
            var scaledWidth = ComputeRoundedDownValue(inWidth, doPowerOf2Rescale, maxValue, roundDownExponent, divisorExponent);
            var scaledHeight = ComputeRoundedDownValue(inHeight, doPowerOf2Rescale, maxValue, roundDownExponent, divisorExponent);

            return (scaledWidth, scaledHeight);
        }
    }
}
