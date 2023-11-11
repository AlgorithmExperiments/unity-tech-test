// For more info: https://forum.unity.com/threads/checking-which-field-changes-in-onvalidate.1198366/

#if UNITY_EDITOR
 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
 
/// <summary>
/// (Editor only)
/// Helper class to track changes of serialized fields from "OnValidate" or "Reset" calls in <see cref="MonoBehaviour"/>, <see cref="ScriptableObject"/>, etc.
/// Create an instance of this class in the object, then call <see cref="TrackFieldChanges"/> with the desired field to track every time "OnValidate" or "Reset" are executed,
/// and its return value will be true if a change has happened.
/// <para>
/// Note that for <see cref="ScriptableObject"/> assets, it's recommended to call <see cref="TrackFieldChanges"/> from "Reset" as well as "OnValidate", since "OnValidate" won't be called when the object is created.
/// </para>
/// <example>
/// <code>
/// FieldChangesTracker changesTracker = new FieldChangesTracker();
/// void OnValidate()
/// {
///     if (changesTracker.TrackFieldChanges(this, x => x.field.subfield))
///         Debug.Log(Changed");
/// }
/// </code>
/// </example>
/// </summary>
 
public class FieldChangesTracker
{
    Dictionary<string, string> lastValuesByFieldPath = new Dictionary<string, string>();
 
 
    /// <summary>
    /// Tracks the current value of a field, and returns whether it has changed from the previously known one.
    /// <para>
    /// Note that for <see cref="ScriptableObject"/> assets, it's recommended to call <see cref="TrackFieldChanges"/> from "Reset" as well as "OnValidate", since "OnValidate" won't be called when the object is created.
    /// </para>
    /// <example>
    /// <code>
    /// FieldChangesTracker changesTracker = new FieldChangesTracker();
    /// void OnValidate()
    /// {
    ///     if (changesTracker.TrackFieldChanges(this, x => x.field.subfield))
    ///         Debug.Log(Changed");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TOwner">The type of the root owner instance.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="rootOwnerInstance">The root owner instance, the <see cref="MonoBehaviour"/>, <see cref="ScriptableObject"/>, etc., that owns the field.</param>
    /// <param name="fieldSelector">Expression to specify the field.</param>
    /// <returns>Whether the field value changed from the last known one. Always false the first time is called for that field after compilation.</returns>
 
    public bool TrackFieldChanges<TOwner, TField>(TOwner rootOwnerInstance, Expression<Func<TOwner, TField>> fieldSelector)
        where TOwner : UnityEngine.Object
    {
        // Get the field info path:
        var fieldInfoPath = GetMemberInfoPath(rootOwnerInstance, fieldSelector);
        if (fieldInfoPath.Count == 0)
        {
            Debug.LogError("No member info path could be retrieved");
            return false;
        }
 
 
        // Get the current field value, and its path as a string to use as key:
        FieldInfo fieldInfo = null;
        object targetObject = rootOwnerInstance;
        string fieldPath = null;
 
        for (int i = 0; i < fieldInfoPath.Count; i++)
        {
            if (fieldInfo != null)
                targetObject = targetObject != null  ?  fieldInfo.GetValue(targetObject)  :  null;
 
            fieldInfo = fieldInfoPath[i] as FieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("One of the members in the field path is not a field");
                return false;
            }
 
            if (fieldInfo.GetCustomAttribute<SerializeReference>(true) != null)
            {
                Debug.LogError($"Fields with the {nameof(SerializeReference)} attribute are not supported for now");
                return false;
            }
 
            if (i > 0)
                fieldPath += ".";
            fieldPath += fieldInfo.Name;
        }
 
        if (targetObject == null)
        {
            // If the final target object is null, the owner instance may not have been initialized,
            // we call the method again after a delay to see if it's initialized then:
            UnityEditor.EditorApplication.delayCall += () => TrackFieldChanges(rootOwnerInstance, fieldSelector);
            return false;
        }
 
        object currentValueObject = fieldInfo.GetValue(targetObject);
 
 
        // If the current value object is null, the owner instance may not have been initialized.
        // We'll set a dummy value for UnityEngine.Object types, or will call the method again after a delay for other types,
        // otherwise in the next call the value will always be considered changed for several field types:
        if (currentValueObject == null)
        {
            Type fieldType = typeof(TField);
 
            if (fieldType == typeof(string))
            {
                currentValueObject = string.Empty;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                currentValueObject = "null";
            }
            else
            {
                UnityEditor.EditorApplication.delayCall += () => TrackFieldChanges(rootOwnerInstance, fieldSelector);
                return false;
            }
        }
         
 
        // Get the current value as a string:
        string currentValueString = null;
 
        if (currentValueObject != null)
        {
            if (currentValueObject is UnityEngine.Object)
            {
                currentValueString = currentValueObject.ToString();
            }
            else
            {
                try
                {
                    currentValueString = JsonUtility.ToJson(currentValueObject);
                }
                catch (Exception)
                {
                    Debug.LogError("Couldn't get the current value with \"JsonUtility.ToJson\"");
                    return false;
                }
 
                if (string.IsNullOrEmpty(currentValueString)  ||  currentValueString == "{}")
                    currentValueString = currentValueObject.ToString();
            }
        }
 
 
        // Check if the value was changed, and store the current value:
        bool changed = lastValuesByFieldPath.TryGetValue(fieldPath, out string lastValue)  &&  lastValue != currentValueString;
 
        lastValuesByFieldPath[fieldPath] = currentValueString;
 
        return changed;
    }
 
 
 
 
    /// <summary>
    /// Retrieves the list of <see cref="MemberInfo"/> of the member returned by the body of the specified expression. For example:
    /// <para><c>GetMembersInfo(instance, x => x.field.subfield)</c></para>
    /// </summary>
    /// <typeparam name="TOwner">The type of the member root owner.</typeparam>
    /// <typeparam name="TMember">The type of the member.</typeparam>
    /// <param name="ownerInstance">The owner instance.</param>
    /// <param name="memberSelector">The expression to select the member.</param>
    /// <returns>The list of members info, from parents to childs; an empty but not null list if they couldn't be retrieved.</returns>
 
    public static List<MemberInfo> GetMemberInfoPath<TOwner, TMember>(TOwner ownerInstance, Expression<Func<TOwner, TMember>> memberSelector)
    {
        Expression body = memberSelector;
        if (body is LambdaExpression lambdaExpression)
        {
            body = lambdaExpression.Body;
        }
 
        List<MemberInfo> membersInfo = new List<MemberInfo>();
        while (body is MemberExpression memberExpression)
        {
            membersInfo.Add(memberExpression.Member);
            body = memberExpression.Expression;
        }
 
        membersInfo.Reverse();
        return membersInfo;
    }
}
 
#endif