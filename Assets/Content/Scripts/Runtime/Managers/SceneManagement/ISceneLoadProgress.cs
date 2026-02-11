using System;

namespace LittleHeroJourney
{
    public interface ISceneLoadProgress
    {
        string SceneId { get; }
        bool IsReady { get; }
        float Progress { get; }
        event Action OnReady;
    }
}
