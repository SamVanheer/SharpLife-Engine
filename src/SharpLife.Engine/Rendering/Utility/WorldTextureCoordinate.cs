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

using System.Numerics;

namespace SharpLife.Engine.Rendering.Utility
{
    /// <summary>
    /// Blittable type used to pass a 3D vertex and 2D texture coordinate to shaders
    /// </summary>
    public struct WorldTextureCoordinate
    {
        public Vector3 Vertex;

        public Vector2 Texture;
    }
}
