using Assets.Scripts.CardEngine.Cards;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomEditor(typeof(ScriptableCard))]
    public sealed class ScriptableCardEditor : UnityEditor.Editor
    {
        private SerializedProperty _id;
        private SerializedProperty _cardName;
        private SerializedProperty _effectText;
        private SerializedProperty _category;
        private SerializedProperty _deployCost;
        private SerializedProperty _power;
        private SerializedProperty _health;
        private SerializedProperty _onPlayEffect;
        private SerializedProperty _rapidEffects;
        private SerializedProperty _ritualStageEffects;

        private void OnEnable()
        {
            _id = serializedObject.FindProperty("id");
            _cardName = serializedObject.FindProperty("cardName");
            _effectText = serializedObject.FindProperty("effectText");
            _category = serializedObject.FindProperty("category");
            _deployCost = serializedObject.FindProperty("deployCost");
            _power = serializedObject.FindProperty("power");
            _health = serializedObject.FindProperty("health");
            _onPlayEffect = serializedObject.FindProperty("onPlayEffect");
            _rapidEffects = serializedObject.FindProperty("rapidEffects");
            _ritualStageEffects = serializedObject.FindProperty("ritualStageEffects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_id);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cardName);
            EditorGUILayout.PropertyField(_effectText);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Gameplay", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_category);

            if ((CardType)_category.enumValueIndex == CardType.Troop)
            {
                EditorGUILayout.PropertyField(_deployCost);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Troop Stats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_power);
                EditorGUILayout.PropertyField(_health);
            }

            EditorGUILayout.PropertyField(_onPlayEffect, includeChildren: true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Rapid Effects", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Rapid effects are optional. Add entries and pick a RapidEffectDefinition type.", MessageType.Info);
            EditorGUILayout.PropertyField(_rapidEffects, includeChildren: true);

            if ((CardType)_category.enumValueIndex == CardType.Ritual)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Ritual Stages", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Stages are only used for Ritual cards. Click a stage element to pick an Effect type.", MessageType.Info);

                EditorGUILayout.PropertyField(_ritualStageEffects, includeChildren: true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
