﻿using System;
using System.Reflection;
using UnityEngine;

namespace BS_OBSControl.Util
{
    public static class ReflectionUtil
    {
        public static void SetPrivateField(this object obj, string fieldName, object value)
        {
            obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
        }

        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            return (T) ((object) obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj));
        }

        public static void SetPrivateProperty(this object obj, string propertyName, object value)
        {
            obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value, null);
        }

        public static void InvokePrivateMethod(this object obj, string methodName, object[] methodParams)
        {
            obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, methodParams);
        }

        public static GameplayCoreSceneSetupData GetSceneData(this GameplayCoreSceneSetup obj, string fieldName)
        {
            return (GameplayCoreSceneSetupData) obj.GetType().BaseType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        public static Component CopyComponent(Component original, Type originalType, Type overridingType, GameObject destination)
        {
            Component component = destination.AddComponent(overridingType);
            FieldInfo[] fields = originalType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo fieldInfo = fields[i];
                fieldInfo.SetValue(component, fieldInfo.GetValue(original));
            }
            return component;
        }

    }
}
