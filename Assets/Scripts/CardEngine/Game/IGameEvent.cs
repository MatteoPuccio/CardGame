
using System;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public interface IGameEvent
    {
        string EventType { get; }
        Card Source { get; }
    }
}