using System;
using Game.MainGame.DTO;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

namespace Game.MainGame
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelTypeText;
        [SerializeField] private Button _button;
        
        [field: SerializeField] public LevelData LevelData { get; private set; }

        public event Action<LevelType> OnOpenClicked;

        public void Initialize(LevelData data)
        {
            LevelData = data ?? throw new ArgumentNullException(nameof(data));
            
            
        }
    }
}