using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class CombatFeedbackUIController : MonoBehaviour
    {
        [SerializeField] private Image hitMarker;
        [SerializeField] private float hitMarkerDuration = 0.08f;

        private float _timer;

        private void Awake()
        {
            if (hitMarker == null)
            {
                hitMarker = CreateHitMarker();
            }

            if (hitMarker != null)
            {
                hitMarker.enabled = false;
            }
        }

        public void ShowHitMarker(bool killed)
        {
            if (hitMarker == null)
            {
                return;
            }

            hitMarker.color = killed ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            hitMarker.enabled = true;
            _timer = Mathf.Max(0.03f, hitMarkerDuration);
        }

        private void Update()
        {
            if (hitMarker == null || !hitMarker.enabled)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                hitMarker.enabled = false;
            }
        }

        private Image CreateHitMarker()
        {
            GameObject marker = new GameObject("HitMarker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(transform, false);
            RectTransform rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(24f, 24f);
            rect.anchoredPosition = Vector2.zero;

            Image image = marker.GetComponent<Image>();
            Texture2D texture = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool diag = x == y || x + y == 7;
                    texture.SetPixel(x, y, diag ? Color.white : new Color(0f, 0f, 0f, 0f));
                }
            }
            texture.Apply();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
            image.raycastTarget = false;
            return image;
        }
    }
}
