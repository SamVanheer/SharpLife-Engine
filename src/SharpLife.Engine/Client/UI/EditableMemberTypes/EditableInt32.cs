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

using FastMember;
using ImGuiNET;
using System;
using System.Reflection;

namespace SharpLife.Engine.Client.UI.EditableMemberTypes
{
    public sealed class EditableInt32 : BaseEditableText
    {
        private int _value;

        public EditableInt32(int index, object editObject, MemberInfo info, Type type, ObjectAccessor objectAccessor)
            : base(index, editObject, info, type, objectAccessor, ImGuiInputTextFlags.CharsDecimal)
        {
            _value = (int)objectAccessor[info.Name];
        }

        public override void Initialize(int index, object editObject, MemberInfo info, ObjectAccessor objectAccessor)
        {
            SetValue(_value.ToString());
        }

        protected override void OnValueChanged(ObjectAccessor objectAccessor, string newValue)
        {
            if (int.TryParse(newValue, out var result) && _value != result)
            {
                objectAccessor[_info.Name] = result;
                _value = result;
            }
            else
            {
                SetValue(_value.ToString());
            }
        }
    }
}
