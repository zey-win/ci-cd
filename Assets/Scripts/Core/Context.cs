using UnityEngine;

namespace Core
{
    public class Context : MonoBehaviour
    {
        private ScoreContext _scoreContext;
        public ScoreContext ReadScoreContext() => _scoreContext;
        public void WriteScoreContext(ScoreContext scoreContext) => _scoreContext = scoreContext;
        public void WriteScoreContext(int score) => _scoreContext.score = score;

        [System.Serializable]
        public struct ScoreContext
        {
            public int score;
        }
    }
}