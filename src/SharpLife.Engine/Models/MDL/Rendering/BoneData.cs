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

using SharpLife.Engine.Models.MDL.FileFormat;
using System;

namespace SharpLife.Engine.Models.MDL.Rendering
{
    public unsafe struct BoneData
    {
        private fixed byte _controllers[MDLConstants.MaxControllers];

        private fixed byte _blenders[MDLConstants.MaxBlenders];

        public byte GetController(int index)
        {
            if (index < 0 || index >= MDLConstants.MaxControllers)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _controllers[index];
        }

        public void SetController(int index, byte value)
        {
            if (index < 0 || index >= MDLConstants.MaxControllers)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _controllers[index] = value;
        }

        public byte GetBlender(int index)
        {
            if (index < 0 || index >= MDLConstants.MaxBlenders)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _blenders[index];
        }

        public void SetBlender(int index, byte value)
        {
            if (index < 0 || index >= MDLConstants.MaxBlenders)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _blenders[index] = value;
        }
    }
}
