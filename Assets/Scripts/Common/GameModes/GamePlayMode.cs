using System;
using Common.Interfaces;
using Cysharp.Threading.Tasks;
using Match3.App;
using Match3.App.Interfaces;
using Match3.Infrastructure.Interfaces;

namespace Common.GameModes
{
    public class GamePlayMode : IGameMode, IDeactivatable
    {
        private readonly UnityGame _unityGame;
        private readonly IGameUiCanvas _gameUiCanvas;
        private readonly IBoardFillStrategy<IUnityGridSlot>[] _boardFillStrategies;

        public GamePlayMode(IAppContext appContext)
        {
            UnityEngine.Debug.Log("GamePlayMode Constructor Called");
            _unityGame = appContext.Resolve<UnityGame>();
            _gameUiCanvas = appContext.Resolve<IGameUiCanvas>();
            _boardFillStrategies = appContext.Resolve<IBoardFillStrategy<IUnityGridSlot>[]>();
        }

        public event EventHandler Finished
        {
            add => _unityGame.Finished += value;
            remove => _unityGame.Finished -= value;
        }

        public void Activate()
        {
            try
            {
                if (_unityGame == null) UnityEngine.Debug.LogError("_unityGame is null!");
                if (_gameUiCanvas == null) UnityEngine.Debug.LogError("_gameUiCanvas is null!");

                _unityGame.LevelGoalAchieved += OnLevelGoalAchieved;
                _gameUiCanvas.StrategyChanged += OnStrategyChanged;

                // Stop Match3 logic
                //_unityGame.SetGameBoardFillStrategy(GetSelectedFillStrategy());
                //_unityGame.StartAsync().Forget();
                
                // Bootstrap Block Blast
                // Ensure single instance
                if (UnityEngine.Object.FindFirstObjectByType<BlockBlastGameManager>() == null)
                {
                    var bbManager = new UnityEngine.GameObject("BlockBlastGameManager").AddComponent<BlockBlastGameManager>();
                    UnityEngine.Debug.Log("BlockBlastGameManager Created by GamePlayMode.");
                }
                
                _gameUiCanvas.ShowMessage("Block Blast Started.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error in GamePlayMode.Activate: {e.Message}\n{e.StackTrace}");
            }
        }

        public void Deactivate()
        {
            _unityGame.LevelGoalAchieved -= OnLevelGoalAchieved;
            _gameUiCanvas.StrategyChanged -= OnStrategyChanged;

            _unityGame.StopAsync().Forget();
            _gameUiCanvas.ShowMessage("Game finished.");
            
            // Cleanup BlockBlast if needed? 
            // Usually scene reload handles it, but for mode switch:
            var manager = UnityEngine.Object.FindFirstObjectByType<BlockBlastGameManager>();
            if (manager != null)
            {
                UnityEngine.Object.Destroy(manager.gameObject);
            }
        }

        private void OnLevelGoalAchieved(object sender, LevelGoal<IUnityGridSlot> levelGoal)
        {
            _gameUiCanvas.RegisterAchievedGoal(levelGoal);
        }

        private void OnStrategyChanged(object sender, int index)
        {
           // _unityGame.SetGameBoardFillStrategy(GetFillStrategy(index));
        }

        private IBoardFillStrategy<IUnityGridSlot> GetSelectedFillStrategy()
        {
            return GetFillStrategy(_gameUiCanvas.SelectedFillStrategyIndex);
        }

        private IBoardFillStrategy<IUnityGridSlot> GetFillStrategy(int index)
        {
            return _boardFillStrategies[index];
        }
    }
}