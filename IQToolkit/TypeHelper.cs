﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit
{
    /// <summary>
    /// Type related helper methods
    /// </summary>
    public static class TypeHelper
    {
        public static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }

        public static Type GetSequenceType(Type elementType)
        {
            return typeof(IEnumerable<>).MakeGenericType(elementType);
        }

        public static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }

        public static bool IsNullableType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullAssignable(Type type)
        {
            return !type.IsValueType || IsNullableType(type);
        }

        public static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        public static Type GetNullAssignableType(Type type)
        {
            if (!IsNullAssignable(type))
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }
            return type;
        }

        public static ConstantExpression GetNullConstant(Type type)
        {
            return Expression.Constant(null, GetNullAssignableType(type));
        }

        public static Type GetMemberType(MemberInfo mi)
        {
            FieldInfo fi = mi as FieldInfo;
            if (fi != null) return fi.FieldType;
            PropertyInfo pi = mi as PropertyInfo;
            if (pi != null) return pi.PropertyType;
            EventInfo ei = mi as EventInfo;
            if (ei != null) return ei.EventHandlerType;
            MethodInfo meth = mi as MethodInfo;  // property getters really
            if (meth != null) return meth.ReturnType;
            return null;
        }

        public static object GetDefault(Type type)
        {
            bool isNullable = !type.IsValueType || TypeHelper.IsNullableType(type);
            if (!isNullable)
                return Activator.CreateInstance(type);
            return null;
        }

        public static bool IsReadOnly(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (((FieldInfo)member).Attributes & FieldAttributes.InitOnly) != 0;
                case MemberTypes.Property:
                    PropertyInfo pi = (PropertyInfo)member;
                    return !pi.CanWrite || pi.GetSetMethod() == null;
                default:
                    return true;
            }
        }

        public static bool IsInteger(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;        
            }
        }
        public static object Convert(object value, Type type)
        {
            //Careful, we may not want to always return the default for a type when a value is null
            if (value == null)
            {
                return GetDefault(type);
            }
            type = GetNonNullableType(type);
            Type vtype = value.GetType();
            if (type != vtype)
            {
                if (type.IsEnum)
                {
                    if (vtype == typeof(string))
                    {
                        return Enum.Parse(type, (string)value);
                    }

                    Type utype = Enum.GetUnderlyingType(type);
                    if (utype != vtype)
                    {
                        value = System.Convert.ChangeType(value, utype);
                    }
                    return Enum.ToObject(type, value);
                }
                //Specifically catch a Guid to String conversion
                if (type == typeof(String) && vtype == typeof(Guid))
                {
                    return value.ToString();
                }
                //Specifically catch a String to Guid conversion
                if (type == typeof(Guid) && vtype == typeof(String))
                {
                    string sVal = value as string;
                    if (sVal == null)
                        throw new InvalidCastException("Couldn't cast value to string.");
                    Guid retVal = new Guid(sVal); //Throws FormatException if the string is not a Guid
                    return retVal;
                }
                //Fallback on System.Convert
                return System.Convert.ChangeType(value, type);
            }
            return value;
        }
    }
}