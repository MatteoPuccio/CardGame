using Assets.Scripts.CardEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Enables picking concrete types for [SerializeReference] fields in the Inspector.
// Unity's default inspector doesn't always expose a type picker, especially for null managed references.

namespace Assets.Editor
{
    internal static class SerializeReferenceTypeCache
    {
        private static readonly Dictionary<Type, Type[]> _derivedTypesCache = new();

        public static Type[] GetConcreteDerivedTypes(Type baseType)
        {
            if (baseType == null)
                return Array.Empty<Type>();

            if (_derivedTypesCache.TryGetValue(baseType, out var cached))
                return cached;

            // TypeCache is editor-only and fast.
            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => t != null && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToArray();

            _derivedTypesCache[baseType] = types;
            return types;
        }

        public static string ToShortName(Type t)
        {
            if (t == null)
                return "(None)";

            return t.Name;
        }
    }

    internal abstract class SerializeReferenceTypePickerDrawerBase : PropertyDrawer
    {
        protected abstract Type BaseType { get; }

        protected virtual void EnsureDefaultInstance(SerializedProperty property) { }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // One line for the type picker + the children (if any).
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property, includeChildren: true);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EnsureDefaultInstance(property);

            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            Type baseType = BaseType;
            Type currentType = property.managedReferenceValue?.GetType();

            var concreteTypes = SerializeReferenceTypeCache.GetConcreteDerivedTypes(baseType);

            // Build popup list: (None) + all concrete types.
            int currentIndex = 0;
            string[] options = new string[concreteTypes.Length + 1];
            options[0] = "(None)";
            for (int i = 0; i < concreteTypes.Length; i++)
            {
                options[i + 1] = SerializeReferenceTypeCache.ToShortName(concreteTypes[i]);
                if (currentType == concreteTypes[i])
                    currentIndex = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(line, label.text, currentIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    Type chosenType = concreteTypes[newIndex - 1];
                    try
                    {
                        property.managedReferenceValue = Activator.CreateInstance(chosenType);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create instance of {chosenType}: {ex}");
                        property.managedReferenceValue = null;
                    }
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            if (property.managedReferenceValue != null)
            {
                Rect body = new(position.x, line.yMax + EditorGUIUtility.standardVerticalSpacing, position.width,
                    EditorGUI.GetPropertyHeight(property, includeChildren: true));

                // Draw the selected object's fields.
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(body, property, GUIContent.none, includeChildren: true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    // Drawers for your specific base types

    [CustomPropertyDrawer(typeof(EffectDefinition), true)]
    internal sealed class EffectDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(EffectDefinition);
    }

    [CustomPropertyDrawer(typeof(EffectSelectorDefinition), true)]
    internal sealed class EffectSelectorDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(EffectSelectorDefinition);
    }

    [CustomPropertyDrawer(typeof(RapidEffectDefinition), true)]
    internal sealed class RapidEffectDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(RapidEffectDefinition);
    }

    [CustomPropertyDrawer(typeof(RapidEffectConditionDefinition), true)]
    internal sealed class RapidEffectConditionDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(RapidEffectConditionDefinition);
    }

    [CustomPropertyDrawer(typeof(CardFilterDefinition), true)]
    internal sealed class CardFilterDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(CardFilterDefinition);
    }

    [CustomPropertyDrawer(typeof(TutorResultActionDefinition), true)]
    internal sealed class TutorResultActionDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(TutorResultActionDefinition);
    }

    [CustomPropertyDrawer(typeof(TriggeredEffectConditionDefinition), true)]
    internal sealed class TriggeredEffectConditionDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(TriggeredEffectConditionDefinition);
    }

    [CustomPropertyDrawer(typeof(AmountDefinition), true)]
    internal sealed class AmountDefinitionTypePickerDrawer : SerializeReferenceTypePickerDrawerBase
    {
        protected override Type BaseType => typeof(AmountDefinition);

        protected override void EnsureDefaultInstance(SerializedProperty property)
        {
            if (property == null)
                return;

            if (property.managedReferenceValue != null)
                return;

            property.managedReferenceValue = new ConstantAmountDefinition { Value = 0 };
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
