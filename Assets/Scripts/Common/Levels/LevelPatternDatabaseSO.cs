using System.Collections.Generic;
using UnityEngine;

namespace Common.Levels
{
    [CreateAssetMenu(fileName = "LevelPatternDatabase", menuName = "Block Blast/Level Pattern Database", order = 2)]
    public class LevelPatternDatabaseSO : ScriptableObject
    {
        public List<LevelPatternSO> patterns = new List<LevelPatternSO>();
    }
}
