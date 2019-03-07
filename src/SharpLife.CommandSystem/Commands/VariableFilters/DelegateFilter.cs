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

using System;

namespace SharpLife.CommandSystem.Commands.VariableFilters
{
    /// <summary>
    /// Filter that delegates the filter task to a delegate
    /// </summary>
    public class DelegateFilter<T> : IVariableFilter<T>
    {
        public delegate bool FilterDelegate(ref VariableChangeEvent<T> changeEvent);

        private readonly FilterDelegate _delegate;

        public DelegateFilter(FilterDelegate @delegate)
        {
            _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        }

        public bool Filter(ref VariableChangeEvent<T> changeEvent)
        {
            return _delegate(ref changeEvent);
        }
    }
}
