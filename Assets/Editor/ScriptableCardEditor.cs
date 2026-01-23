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
        private SerializedProperty _tags;
        private SerializedProperty _deployCost;
        private SerializedProperty _power;
        private SerializedProperty _health;
        private SerializedProperty _hasRace;
        private SerializedProperty _race;
        private SerializedProperty _keywords;
        private SerializedProperty _spellSchool;
        private SerializedProperty _triggeredEffects;
        private SerializedProperty _rapidEffects;
        private SerializedProperty _ritualStageEffects;

        private void OnEnable()
        {
            _id = serializedObject.FindProperty("id");
            _cardName = serializedObject.FindProperty("cardName");
            _effectText = serializedObject.FindProperty("effectText");
            _category = serializedObject.FindProperty("category");
            _tags = serializedObject.FindProperty("tags");
            _deployCost = serializedObject.FindProperty("deployCost");
            _power = serializedObject.FindProperty("power");
            _health = serializedObject.FindProperty("health");
            _hasRace = serializedObject.FindProperty("hasRace");
            _race = serializedObject.FindProperty("race");
            _keywords = serializedObject.FindProperty("keywords");
            _spellSchool = serializedObject.FindProperty("spellSchool");
            _triggeredEffects = serializedObject.FindProperty("triggeredEffects");
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

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Tags / Archetype", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Optional tags (case-insensitive). Useful for archetype-based effects like tutors.", MessageType.Info);
            if (_tags != null)
                EditorGUILayout.PropertyField(_tags, includeChildren: true);

            var category = (CardType)_category.enumValueIndex;

            if (category == CardType.Troop)
            {
                EditorGUILayout.PropertyField(_deployCost);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Troop Stats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_power);
                EditorGUILayout.PropertyField(_health);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Troop Race", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Pick None for no race.", MessageType.Info);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_race);
                if (EditorGUI.EndChangeCheck() && _hasRace != null && _race != null)
                {
                    string selectedName = _race.enumNames[_race.enumValueIndex];
                    _hasRace.boolValue = selectedName != "None";
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Troop Keywords", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Optional. Keywords are only used for Troop cards.", MessageType.Info);
                EditorGUILayout.PropertyField(_keywords, includeChildren: true);
            }

            if (category == CardType.Spell)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Spell School", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_spellSchool);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Triggered Effects", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Triggered effects are generic (e.g., WhenThisIsPlayed). Add entries and pick an Effect + Condition type. Optional is set on the triggered-effect entry.", MessageType.Info);
            if (_triggeredEffects != null)
                EditorGUILayout.PropertyField(_triggeredEffects, includeChildren: true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Rapid Effects", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Rapid effects are optional. Add entries and pick a RapidEffectDefinition type. Optional is set on the rapid-effect entry.", MessageType.Info);
            EditorGUILayout.PropertyField(_rapidEffects, includeChildren: true);

            if (category == CardType.Ritual)
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
