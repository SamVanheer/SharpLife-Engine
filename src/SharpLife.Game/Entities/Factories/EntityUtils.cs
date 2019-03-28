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

using SharpLife.Engine.Entities;
using SharpLife.Engine.Entities.Components;
using SharpLife.Utility.Mathematics;
using System.Numerics;

namespace SharpLife.Game.Entities.Factories
{
    public static class EntityUtils
    {
        /// <summary>
        /// Constant for "up" move direction as provided by Hammer
        /// </summary>
        public static readonly Vector3 Up = new Vector3(0, -1, 0);

        /// <summary>
        /// Constant for "down" move direction as provided by Hammer
        /// </summary>
        public static readonly Vector3 Down = new Vector3(0, -2, 0);

        /// <summary>
        /// QuakeEd only writes a single float for angles (bad idea), so up and down are just constant angles.
        /// </summary>
        /// <param name="transform"></param>
        public static void SetMoveDir(Transform transform)
        {
            if (transform.Angles == Up)
            {
                transform.MoveDirection = new Vector3(0, 0, 1);
            }
            else if (transform.Angles == Down)
            {
                transform.MoveDirection = new Vector3(0, 0, -1);
            }
            else
            {
                VectorUtils.AngleToVectors(transform.Angles, out var forward, out _, out _);
                transform.MoveDirection = forward;
            }

            transform.Angles = Vector3.Zero;
        }

        public static bool IsMasterTriggered(string sMaster, Entity pActivator)
        {
            //TODO: implement
            /*
            if (sMaster != null)
            {
                var pentTarget = FIND_ENTITY_BY_TARGETNAME(NULL, STRING(sMaster));

                if (!FNullEnt(pentTarget))
                {
                    BaseEntity pMaster = CBaseEntity::Instance(pentTarget);
                    if (pMaster && (pMaster->ObjectCaps() & FCAP_MASTER))
                        return pMaster->IsTriggered(pActivator);
                }

                ALERT(at_console, "Master was null or not a master!\n");
            }
            */
            // if this isn't a master entity, just say yes.
            return true;
        }
    }
}
