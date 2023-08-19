using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Warlander.Core;
using Warlander.Deedplanner.Data;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class LoadingSceneController : WarlanderBehaviour
    {
        [Inject] private ZenjectSceneLoader _sceneLoader;
        [Inject] private DataLoader _dataLoader;

        private const float ProgressAfterLoadingData = 0.5f;
        
        [SerializeField] private CanvasGroup _splashGroup = null;
        
        [SerializeField] private TMP_Text _loadingText = null;
        [SerializeField] private Slider _loadingBar = null;

        private AsyncOperation _sceneLoadingOperation;
        
        private void Start()
        {
            if (_dataLoader.Completed)
            {
                OnLoadingDone();
            }
            
            _dataLoader.LoadingStepStarted += DataLoaderOnLoadingStepStarted;
            _dataLoader.LoadingComplete += DataLoaderOnLoadingComplete;
        }
        
        private void DataLoaderOnLoadingStepStarted(int stepNumber, string stepDescription)
        {
            float progressStep = ProgressAfterLoadingData / DataLoader.TotalSteps;
            float progressToShow = progressStep * stepNumber;

            _loadingBar.value = progressToShow;
            _loadingText.text = stepDescription;
        }
        
        private void DataLoaderOnLoadingComplete()
        {
            OnLoadingDone();
        }

        private void OnLoadingDone()
        {
            _loadingBar.value = ProgressAfterLoadingData;
            _loadingText.text = "Launching";

            _sceneLoadingOperation = _sceneLoader.LoadSceneAsync(SceneNames.MainScene, LoadSceneMode.Additive);
            _sceneLoadingOperation.completed += SceneLoadingOperationOnCompleted;
        }

        private void SceneLoadingOperationOnCompleted(AsyncOperation obj)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.MainScene));
            
            _loadingBar.value = 1.0f;
            _loadingText.text = "Loading complete";
            
            _splashGroup.DOFade(0, 1f).OnComplete(() =>
            {
                SceneManager.UnloadSceneAsync(SceneNames.LoadingScene);
            });
        }

        private void Update()
        {
            if (_sceneLoadingOperation != null)
            {
                _loadingBar.value = Mathf.Lerp(ProgressAfterLoadingData, 1, _sceneLoadingOperation.progress);
            }
        }
    }
}