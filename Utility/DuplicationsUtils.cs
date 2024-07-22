using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace VLib
{
    public interface IReflectionCloneable<T> where T : class, new() { }

    public static class IReflectionCloneableExt
    {
        public static T CloneWithReflection<T>(this T obj)
            where T : class, new()
        {
            T clone = new T();
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                field.SetValue(clone, field.GetValue(obj));
            }
            // Properties
            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (property.CanWrite)
                    property.SetValue(clone, property.GetValue(obj));
            }
            return clone;
        }
    }
    
    public static class DuplicationsUtils
    {
        /// <summary> WARNING: Doesn't work any objects that aren't properly serializable! </summary> 
        public static T DuplicateSerialized<T>(this T serializableObject)
        {
            if (serializableObject == null)
                return default;

            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, serializableObject);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
        
        /// <summary> Based on the input type, create a new instance of that type. </summary> 
        public static T CreateNewInstance<T>(this T instance) where T : new() => new();

        /// <summary> Creates a new array and uses the 'new()' constraint to create new instances of each element. </summary> 
        public static T[] CloneArrayWithNewInstances<T>(this T[] array)
            where T : new()
        {
            if (array == null)
            {
                Debug.LogError("Cannot duplicate null array");
                return null;
            }

            var newArray = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
                newArray[i] = array[i].CreateNewInstance();

            return newArray;
        }
        
        /// <summary> Extension method that deep clones an array of class instances</summary> 
        public static T[] CloneArrayWithNewClassInstances<T>(this T[] instanceArray) 
            where T : class
        {
            var newArray = new T[instanceArray.Length];
            for (var i = 0; i < instanceArray.Length; i++)
            {
                newArray[i] = Activator.CreateInstance(instanceArray[i].GetType()) as T;
                if (newArray[i] == null)
                    Debug.LogError($"Failed to create new instance of {instanceArray[i].GetType()} and cast it to {typeof(T)}");
            }

            return newArray;
        }
    }
}