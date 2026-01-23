using UnityEngine;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Keywords;
using Assets.Scripts.CardEngine.Rules;

namespace Assets.Scripts.CardEngine.Cards
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ScriptableCard", order = 1)]
    public class ScriptableCard : ScriptableObject
    {
        [Header("Identity")]
        public string id;

        [Header("Display")]
        public string cardName;

        [TextArea]
        public string effectText;

        [Header("Gameplay")]
        public CardType category = CardType.Troop;

        [Header("Tags / Archetype")]
        [Tooltip("Optional tags (case-insensitive). Use these for archetypes like 'Dragon', 'Necromancy', etc.")]
        public List<string> tags = new();

        [Min(0)]
        public int deployCost = 1;

        [Header("Troop Stats")]
        [Tooltip("Only used when category = Troop.")]
        [Min(0)]
        public int power = 0;

        [Tooltip("Only used when category = Troop.")]
        [Min(1)]
        public int health = 1;

        [Header("Troop Race")]
        [Tooltip("Optional. Only used when category = Troop. Use None for no race.")]
        public TroopRaces race = TroopRaces.None;


        [Header("Troop Keywords")]
        [Tooltip("Optional. Only used when category = Troop.")]
        public List<CardKeyword> keywords = new();

        [Header("Spell School")]
        [Tooltip("Only used when category = Spell.")]
        public SpellSchool spellSchool = SpellSchool.None;

        [Header("Triggered Effects")]
        [Tooltip("Generic triggered effects (e.g., WhenThisIsPlayed). Optional.")]
        [SerializeField]
        public List<TriggeredEffectDefinition> triggeredEffects = new();

        [Header("Rapid Effects")]
        [Tooltip("Optional fast/response effects that can be activated during chain windows.")]
        [SerializeReference]
        public List<RapidEffectDefinition> rapidEffects = new();

        [Header("Ritual Stages")]
        [Tooltip("Only used when category = Ritual. Each stage effect triggers once per Ritual phase, starting immediately when the ritual is set up.")]
        [SerializeReference]
        public List<EffectDefinition> ritualStageEffects = new();

        private void AddRapidEffectsTo(Card card)
        {
            if (card == null || rapidEffects == null || rapidEffects.Count == 0)
                return;

            foreach (var def in rapidEffects)
            {
                if (def == null)
                    continue;

                var rapid = def.CreateRuntimeRapidEffect();
                if (rapid != null)
                    card.RapidEffects.Add(rapid);
            }
        }

        private void ConfigureTroopStats(Card card)
        {
            if (card?.Behavior is not TroopBehavior troop)
                return;

            troop.DeployCost = deployCost;
            troop.InitializeStats(power, health);

            troop.SetRace(race);
            var raceKeywords = race == TroopRaces.None ? null : RaceRegistry.GetGrantedKeywords(race);
            troop.SetKeywords(keywords, raceKeywords);
        }

        private void ConfigureRitualStages(Card card)
        {
            if (card?.Behavior is not RitualBehavior ritual)
                return;

            if (ritualStageEffects == null || ritualStageEffects.Count == 0)
                return;

            var stages = new List<Effect>(ritualStageEffects.Count);
            int stageIndex = 0;
            foreach (var def in ritualStageEffects)
            {
                if (def == null)
                    continue;

                if (TryAddRitualRapidEffect(card, def, stageIndex, stages))
                {
                    stageIndex++;
                    continue;
                }

                if (TryAddRitualStageEffect(def, stages))
                    stageIndex++;
            }
            ritual.SetStages(stages);
        }

        private static bool TryAddRitualRapidEffect(Card card, EffectDefinition def, int stageIndex, List<Effect> stages)
        {
            // If a ritual-stage entry is actually a rapid effect, treat it as a rapid prompt option,
            // gated to the ritual's current stage index.
            if (def is not RapidEffectDefinition rapidDef)
                return false;

            // Preserve stage numbering: a rapid-only stage still occupies a stage slot.
            stages?.Add(new NoOpEffect());

            var rapid = rapidDef.CreateRuntimeRapidEffect();
            if (rapid?.InnerEffect == null)
                return true;

            var combinedConditions = new List<IRapidEffectCondition>();
            if (rapid.Conditions != null)
                combinedConditions.AddRange(rapid.Conditions);

            combinedConditions.Add(new RitualStageEqualsRapidCondition(stageIndex));
            combinedConditions.Add(new RitualNotAdvancedThisTurnRapidCondition());
            combinedConditions.Add(new RitualMustBeInPlayRapidCondition());

            card?.RapidEffects?.Add(new RapidEffect(rapid.InnerEffect, combinedConditions, rapid.ActivationFrequency));
            return true;
        }

        private sealed class NoOpEffect : Effect, IPlaceholderEffect
        {
            protected override void ResolveCore(EffectContext effectContext)
            {
            }
        }

        private static bool TryAddRitualStageEffect(EffectDefinition def, List<Effect> stages)
        {
            var eff = def?.CreateRuntimeEffect();
            if (eff == null)
                return false;

            stages?.Add(eff);
            return true;
        }

        public Card CreateRuntimeCard(Player owner, GameState gameState = null)
        {
            if (owner == null)
            {
                Debug.LogError("ScriptableCard: CreateRuntimeCard called with null owner.");
                return null;
            }

            string runtimeId = string.IsNullOrWhiteSpace(id) ? name : id;
            string runtimeName = string.IsNullOrWhiteSpace(cardName) ? name : cardName;

            var card = new Card(
                id: runtimeId,
                cardCategory: category,
                owner: owner,
                name: runtimeName,
                effectText: effectText ?? string.Empty,
                gameState: gameState,
                tags: tags
            );

            if (triggeredEffects != null)
            {
                for (int i = 0; i < triggeredEffects.Count; i++)
                {
                    var def = triggeredEffects[i];
                    var runtime = def != null ? def.CreateRuntimeTriggeredEffect() : null;
                    if (runtime != null)
                        card.TriggeredEffects.Add(runtime);
                }
            }

            if (card.Behavior is SpellBehavior spell)
                spell.SetSchool(spellSchool);

            AddRapidEffectsTo(card);
            ConfigureTroopStats(card);
            ConfigureRitualStages(card);

            return card;
        }
    }
}