using UnityEngine;
using System.Collections;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    public class DrawingReplay : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private DrawingSurface targetSurface;

        [Header("Replay Settings")]
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool loopReplay = false;

        private DrawingData replayData;
        private bool isPlaying = false;
        private Coroutine replayCoroutine;

        public bool IsPlaying => isPlaying;

        public void StartReplay(DrawingData data)
        {
            if (targetSurface == null || data == null) return;

            StopReplay();

            replayData = data.Clone();
            targetSurface.Clear();

            replayCoroutine = StartCoroutine(ReplayRoutine());
        }

        public void StopReplay()
        {
            if (replayCoroutine != null)
            {
                StopCoroutine(replayCoroutine);
                replayCoroutine = null;
            }
            isPlaying = false;
        }

        private IEnumerator ReplayRoutine()
        {
            isPlaying = true;

            do
            {
                foreach (var stroke in replayData.strokes)
                {
                    Stroke activeStroke = new Stroke(stroke.color, stroke.width, stroke.toolId);
                    targetSurface.Data.AddStroke(activeStroke);

                    foreach (var point in stroke.points)
                    {
                        activeStroke.AddPoint(point);
                        yield return new WaitForSeconds(0.01f / playbackSpeed);
                    }

                    yield return new WaitForSeconds(0.1f / playbackSpeed);
                }

                if (loopReplay)
                {
                    targetSurface.Clear();
                    yield return new WaitForSeconds(1f);
                }

            } while (loopReplay);

            isPlaying = false;
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetTargetSurface(DrawingSurface surface)
        {
            targetSurface = surface;
        }
    }
}
