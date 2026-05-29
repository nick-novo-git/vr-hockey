using UnityEngine;

namespace HockeyStickhandling
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private float levelDurationSeconds = 45.0f;

        private int score;
        private int hits;
        private float startTime;
        private TextMesh statusText;
        private Transform statusRoot;
        private string preGameMessage;

        public float ElapsedSeconds => Mathf.Max(0.0f, Time.time - startTime);
        public int Level => Mathf.Max(1, Mathf.FloorToInt(ElapsedSeconds / Mathf.Max(1.0f, levelDurationSeconds)) + 1);
        public float Difficulty => Mathf.Clamp01((Level - 1) / 8.0f);

        private void Awake()
        {
            startTime = Time.time;
        }

        public void BeginRun()
        {
            score = 0;
            hits = 0;
            startTime = Time.time;
            preGameMessage = string.Empty;
        }

        public void SetPreGameMessage(string message)
        {
            preGameMessage = message;
        }

        public void AttachWorldStatus(Transform parent)
        {
            if (statusRoot != null)
            {
                statusRoot.SetParent(parent, false);
                statusRoot.localPosition = new Vector3(-4.0f, 2.2f, 23.0f);
                statusRoot.localRotation = Quaternion.Euler(18.0f, 0.0f, 0.0f);
                return;
            }

            CreateStatusText(parent);
        }

        public void AttachStickTracker(StickTracker tracker)
        {
        }

        public void RegisterDodge()
        {
            score += 1;
        }

        public void RegisterHit()
        {
            hits += 1;
            score = Mathf.Max(0, score - 5);
        }

        private void LateUpdate()
        {
            if (statusText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(preGameMessage))
            {
                statusText.text = preGameMessage;
                return;
            }

            var elapsed = Mathf.FloorToInt(ElapsedSeconds);
            var levelTime = Mathf.FloorToInt(ElapsedSeconds % Mathf.Max(1.0f, levelDurationSeconds));
            var nextLevel = Mathf.CeilToInt(Mathf.Max(0.0f, levelDurationSeconds - levelTime));
            statusText.text = $"Level {Level}\nScore {score}\nHits {hits}\nTime {elapsed}s\nNext {nextLevel}s";
        }

        private void CreateStatusText(Transform parent)
        {
            var textObject = new GameObject("VR Status Text");
            statusRoot = textObject.transform;
            statusRoot.SetParent(parent, false);
            statusRoot.localPosition = new Vector3(-4.0f, 2.2f, 23.0f);
            statusRoot.localRotation = Quaternion.Euler(18.0f, 0.0f, 0.0f);

            statusText = textObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.characterSize = 0.08f;
            statusText.fontSize = 48;
            statusText.color = Color.black;
        }
    }
}
