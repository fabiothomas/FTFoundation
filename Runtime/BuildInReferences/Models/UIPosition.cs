using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    public struct UIPosition
    {
        private UIStretch? verticalStretch;
        private UIStretch? horizontalStretch;
        private UIAnchored? verticalAnchored;
        private UIAnchored? horizontalAnchored;

        public static UIPosition FullScreen => new()
        {
            verticalStretch = new() { anchorMin = 0f, anchorMax = 1f, offsetMin = 0f, offsetMax = 0f },
            horizontalStretch = new() { anchorMin = 0f, anchorMax = 1f, offsetMin = 0f, offsetMax = 0f },
        };

        public static UIPosition Get(
            UIStretch horizontalStretch,
            UIStretch verticalStretch
        ) => new()
        {
            horizontalStretch = horizontalStretch,
            verticalStretch = verticalStretch,
        };

        public static UIPosition Get(
            UIAnchored horizontalAnchored,
            UIAnchored verticalAnchored
        ) => new()
        {
            horizontalAnchored = horizontalAnchored,
            verticalAnchored = verticalAnchored,
        };

        public static UIPosition Get(
            UIStretch horizontalStretch,
            UIAnchored verticalAnchored
        ) => new()
        {
            horizontalStretch = horizontalStretch,
            verticalAnchored = verticalAnchored,
        };

        public static UIPosition Get(
            UIAnchored horizontalAnchored,
            UIStretch verticalStretch
        ) => new()
        {
            horizontalAnchored = horizontalAnchored,
            verticalStretch = verticalStretch,
        };

        public void SetTransform(RectTransform transform)
        {
            if (horizontalStretch.HasValue && verticalStretch.HasValue)
            {
                transform.anchorMin = new Vector2(horizontalStretch.Value.anchorMin, verticalStretch.Value.anchorMin);
                transform.anchorMax = new Vector2(horizontalStretch.Value.anchorMax, verticalStretch.Value.anchorMax);
                transform.offsetMin = new Vector2(horizontalStretch.Value.offsetMin, verticalStretch.Value.offsetMin);
                transform.offsetMax = new Vector2(horizontalStretch.Value.offsetMax, verticalStretch.Value.offsetMax);
            }
            else if (horizontalAnchored.HasValue && verticalAnchored.HasValue)
            {
                transform.anchorMin = new Vector2(horizontalAnchored.Value.anchor, verticalAnchored.Value.anchor);
                transform.anchorMax = new Vector2(horizontalAnchored.Value.anchor, verticalAnchored.Value.anchor);
                transform.anchoredPosition = new Vector2(horizontalAnchored.Value.offset, verticalAnchored.Value.offset);
                transform.sizeDelta = new Vector2(horizontalAnchored.Value.size, verticalAnchored.Value.size);
            }
            else if (horizontalStretch.HasValue && verticalAnchored.HasValue)
            {
                transform.anchorMin = new Vector2(horizontalStretch.Value.anchorMin, verticalAnchored.Value.anchor);
                transform.anchorMax = new Vector2(horizontalStretch.Value.anchorMax, verticalAnchored.Value.anchor);
                transform.offsetMin = new Vector2(horizontalStretch.Value.offsetMin, transform.offsetMin.y);
                transform.offsetMax = new Vector2(horizontalStretch.Value.offsetMax, transform.offsetMax.y);
                transform.anchoredPosition = new Vector2(transform.anchoredPosition.x, verticalAnchored.Value.offset);
                transform.sizeDelta = new Vector2(transform.sizeDelta.x, verticalAnchored.Value.size);
            }
            else if (horizontalAnchored.HasValue && verticalStretch.HasValue)
            {
                transform.anchorMin = new Vector2(horizontalAnchored.Value.anchor, verticalStretch.Value.anchorMin);
                transform.anchorMax = new Vector2(horizontalAnchored.Value.anchor, verticalStretch.Value.anchorMax);
                transform.offsetMin = new Vector2(transform.offsetMin.x, verticalStretch.Value.offsetMin);
                transform.offsetMax = new Vector2(transform.offsetMax.x, verticalStretch.Value.offsetMax);
                transform.anchoredPosition = new Vector2(horizontalAnchored.Value.offset, transform.anchoredPosition.y);
                transform.sizeDelta = new Vector2(horizontalAnchored.Value.size, transform.sizeDelta.y);
            }
        }
    }

    public struct UIStretch
    {
        public float anchorMin;
        public float anchorMax;
        public float offsetMin;
        public float offsetMax;
    }

    public struct UIAnchored
    {
        public float anchor;
        public float offset;
        public float size;
    }
}