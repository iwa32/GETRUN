using UnityEngine;
using Zenject;
using GameModel;

namespace GameInstaller
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IGameModel>().To<GameModel.GameModel>().AsSingle();
            Container.Bind<IPointModel>().To<PointModel>().AsSingle();
            Container.Bind<IScoreModel>().To<ScoreModel>().AsSingle();
        }
    }
}