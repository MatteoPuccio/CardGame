// using Assets.Scripts.CardEngine.Events;
// using Assets.Scripts.CardEngine.Cards;
// namespace Assets.Scripts.CardEngine.Effects
// {
//     public class OnPlayEffect
//     {
//         private readonly Card _card;
//         private readonly IEffect _effect;
// 
//         public OnPlayEffect(Card card, IEffect effect, EventBus eventBus)
//         {
//             _card = card;
//             _effect = effect;
// 
//             eventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
//         }
// 
//         private void OnCardPlayed(CardPlayedEvent evt)
//         {
//             if (evt.Card != _card)
//                 return;
// 
//             EffectContext context = new EffectContext
//             {
//                 Source = _card,
//                 GameState = _card.GameState,
//                 TriggeringEvent = evt
//             };
// 
//             _effect.Resolve(context);
//         }
//     }
// }
