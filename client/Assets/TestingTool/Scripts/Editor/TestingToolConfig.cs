using UnityEngine;

namespace TestingTool.Scripts
{
    public class TestingToolConfig : ScriptableObject
    {
        public bool IsTestingToolEnabled => enableTestingTool;
        
        [SerializeField] private bool enableTestingTool = false;
    }
}