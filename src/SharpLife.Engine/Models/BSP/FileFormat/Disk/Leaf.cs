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


namespace SharpLife.Engine.Models.BSP.FileFormat.Disk
{
    internal unsafe struct Leaf
    {
#pragma warning disable CS0649
        public Contents contents;
        public int visofs;             // -1 = no visibility info

        public fixed short mins[3];          // for frustum culling
        public fixed short maxs[3];

        public ushort firstmarksurface;
        public ushort nummarksurfaces;

        public fixed byte ambient_level[(int)Ambient.LastAmbient + 1];
#pragma warning restore CS0649
    }
}
