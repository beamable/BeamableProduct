using System.Collections.Generic;
using UnityEngine;

namespace TestingTool.Scripts.Helpers
{
    public static class ColorHelper
    {
        private static readonly Dictionary<ProgressStatus, Color> _colors = new Dictionary<ProgressStatus, Color>()
        {
            { ProgressStatus.NotSet, Color.gray },
            { ProgressStatus.NotPassed, Color.red },
            { ProgressStatus.Pending, Color.yellow },
            { ProgressStatus.Passed, Color.green },
        };
            
        public static Color GetColor(ProgressStatus status) => _colors[status];
    }
}