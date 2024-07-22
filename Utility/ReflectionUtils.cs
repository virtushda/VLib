using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.Utilities;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public static class ReflectionUtils
    {
        public static Type FieldOrPropType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo f)
                return f.FieldType;
            if (memberInfo is PropertyInfo p)
                return p.PropertyType;
            throw new InvalidOperationException();
        }
        
        /// <summary>Get all fields of TDetectType on object of TSourceType</summary> 
        public static List<TDetectType> GetFieldsOfType<TSourceType, TDetectType>(this TSourceType source, bool scanForIListElements)
        {
            var animDataFields = new List<TDetectType>();

            var fields = typeof(TSourceType).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
            object sourceBoxed = source;
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(sourceBoxed);
                var fieldType = field.FieldType;
                
                if (fieldValue is TDetectType animDataField)
                    animDataFields.Add(animDataField);

                // If is array or list
                if (scanForIListElements && fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    var itemType = fieldType.GetGenericArguments()[0];
                    if (itemType == typeof(TDetectType))
                    {
                        var list = fieldValue as IList<TDetectType>;
                        if (list == null)
                            throw new NullReferenceException($"List of type {itemType} was null");
                        foreach (var item in list)
                            animDataFields.Add(item);
                    }
                }
            }

            return animDataFields;
        }
        
        public static Type TypeOfList<T>(this IList<T> list) => typeof(T);

        /// <summary> Use reflection to analyze fields and properties, copying [parent -> child] where [name and type] match. </summary> 
        public static TChild ReflectionCopyOf<TParent, TChild>(this TParent parent, bool copyFields = true, bool copyProperties = true,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            where TParent : class
            where TChild : class, new()
        {
            var child = new TChild();
            parent.ReflectionCopyTo(child, copyFields, copyProperties, bindingFlags);
            return child;
        }

        /// <summary> Use reflection to analyze fields and properties, copying [parent -> child] where [name and type] match. </summary> 
        public static void ReflectionCopyTo<TParent, TChild>(this TParent parent, TChild child, bool copyFields = true, bool copyProperties = true,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            where TParent : class
            where TChild : class
        {
            ReflectionCopier<TParent, TChild>.CopyPropsFieldsByNameType(parent, child, copyFields, copyProperties, bindingFlags);
        }

        public class ReflectionCopier<TParent, TChild>
            where TParent : class
            where TChild : class
        {
            public static void CopyPropsFieldsByNameType(TParent parent, TChild child, bool copyFields = true, bool copyProperties = true,
                                                         BindingFlags bindingFlags = BindingFlags.Public)
            {
                //Properties
                if (copyProperties)
                {
                    var parentProperties = parent.GetType().GetProperties(bindingFlags);
                    var childProperties = child.GetType().GetProperties(bindingFlags);

                    CopyPropsFieldsByNameTypeInternal(parent, child, parentProperties, childProperties);
                }

                //Fields
                if (copyFields)
                {
                    var parentProperties = parent.GetType().GetFields(bindingFlags);
                    var childProperties = child.GetType().GetFields(bindingFlags);

                    CopyPropsFieldsByNameTypeInternal(parent, child, parentProperties, childProperties);
                }
            }

            /// <summary> Only Member types PropertyInfo and FieldInfo are supported! </summary>
            public static void CopyPropsFieldsByNameTypeInternal(TParent parent, TChild child, MemberInfo[] parentMembers, MemberInfo[] childMembers)
            {
                foreach (var parentMember in parentMembers)
                {
                    foreach (var childMember in childMembers)
                    {
                        try
                        {
                            if (parentMember is PropertyInfo parentProperty && !parentProperty.CanRead)
                                continue;
                            if (childMember is PropertyInfo childProperty && !childProperty.CanWrite)
                                continue;

                            if (parentMember.FieldOrPropType() == childMember.FieldOrPropType() 
                                && parentMember.Name == childMember.Name)
                            {
                                childMember.SetMemberValue(child, parentMember.GetMemberValue(parent));
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        public class ReflectionReader<ReadType>
        {
            public static void GetAllFieldsOfTypeOnObject<T>(ReadType readFrom, ref List<T> collection,
                                                             BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance,
                                                             bool exactType = false)
            {
                if (collection == null)
                    collection = new List<T>();
                collection.Clear();

                FieldInfo[] fields = readFrom.GetType().GetFields(bindingFlags);

                for (int i = 0; i < fields.Length; i++)
                {
                    bool isType = fields[i].FieldType is T;
                    bool isCastableTo = !exactType && fields[i].FieldType.IsCastableTo(typeof(T));
                    if (isType || isCastableTo)
                    {
                        T fieldValue = (T)(fields[i].GetValue(readFrom));
                        collection.Add(fieldValue);
                    }
                }

                /*IEnumerable<FieldInfo> fieldsOfType = fields.Where((f) => f is T || !exactType && f.GetType().InheritsFrom<T>());

                for (int i = 0; i < fieldsOfType.Count(); i++)
                {
                    T element = (T)fieldsOfType.ElementAt(i).GetValue(readFrom);
                    collection.Add(element);
                }*/
            }
        }
    }
}