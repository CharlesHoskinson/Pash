﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Extensions.Reflection
{
    static class _
    {
        public static T GetValue<T>(this FieldInfo fieldInfo, object obj = null)
        {
            return (T)fieldInfo.GetValue(obj);
        }

        public static bool IsAssignableFrom<T>(this Type @this, T t)
        {
            return @this.IsAssignableFrom(t.GetType());
        }

        /// <summary>
        /// If `t` is a base class, how many levels up is it?
        /// </summary>
        public static int GetDerivationRank<T>(this Type @this, T t)
        {
            int count = 0;

            var tType = t.GetType();

            while (@this != tType)
            {
                tType = tType.BaseType;
                if (tType == null) throw new Exception();
                count++;
            }

            return count;
        }
    }
}
