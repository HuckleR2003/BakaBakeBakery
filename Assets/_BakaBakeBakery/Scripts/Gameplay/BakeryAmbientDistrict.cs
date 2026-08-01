using System.Collections.Generic;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BakeryAmbientDistrict : MonoBehaviour
    {
        private readonly List<Transform> windowLights = new();
        private readonly List<Transform> chimneyPuffs = new();
        private readonly List<Transform> flourMotes = new();
        private readonly List<Transform> shutterSlats = new();
        private BakeryGameController game;
        private Transform shutter;
        private Transform bicycle;
        private Transform deliveryCar;
        private Transform friend;
        private Vector3 friendBase;
        private float nextWindowChange = 2f;
        private int windowCursor;

        private void Start()
        {
            game = FindAnyObjectByType<BakeryGameController>();
            foreach (var item in GetComponentsInChildren<Transform>(true))
            {
                if (item.name.StartsWith("Window Light")) windowLights.Add(item);
                else if (item.name.StartsWith("Backdrop Smoke")) chimneyPuffs.Add(item);
                else if (item.name.StartsWith("Flour Mote")) flourMotes.Add(item);
                else if (item.name.StartsWith("Wooden Shutter Slat")) shutterSlats.Add(item);
                else if (item.name == "Service Shutter") shutter = item;
                else if (item.name == "Morning Bicycle") bicycle = item;
                else if (item.name == "Old Delivery Car") deliveryCar = item;
                else if (item.name == "Friend - Mila") friend = item;
            }

            if (friend != null) friendBase = friend.localPosition;
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            AnimateWindows(time);
            AnimateSmoke(time);
            AnimateFlour(time);
            AnimateBakeryState(time);
        }

        private void AnimateWindows(float time)
        {
            if (windowLights.Count == 0 || time < nextWindowChange)
            {
                return;
            }

            nextWindowChange = time + 2.6f + windowCursor % 3 * 0.7f;
            var window = windowLights[windowCursor % windowLights.Count];
            window.gameObject.SetActive(!window.gameObject.activeSelf);
            windowCursor++;
        }

        private void AnimateSmoke(float time)
        {
            for (var index = 0; index < chimneyPuffs.Count; index++)
            {
                var puff = chimneyPuffs[index];
                var phase = Mathf.Repeat(time * 0.16f + index * 0.31f, 1f);
                puff.localPosition = new Vector3(
                    Mathf.Sin(time * 0.7f + index) * 0.18f,
                    0.45f + phase * 1.6f,
                    Mathf.Cos(time * 0.43f + index) * 0.08f);
                puff.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.48f, phase);
            }
        }

        private void AnimateFlour(float time)
        {
            var working = game != null && game.CurrentSnapshot.Phase == BakeryWorkPhase.FetchingDough;
            for (var index = 0; index < flourMotes.Count; index++)
            {
                var mote = flourMotes[index];
                mote.gameObject.SetActive(working);
                if (!working) continue;
                var phase = Mathf.Repeat(time * 0.52f + index * 0.17f, 1f);
                mote.localPosition = new Vector3(
                    -0.65f + index * 0.24f,
                    1.34f + phase * 0.75f,
                    -0.28f + Mathf.Sin(time * 1.4f + index) * 0.12f);
                mote.localScale = Vector3.one * Mathf.Lerp(0.055f, 0.018f, phase);
            }
        }

        private void AnimateBakeryState(float time)
        {
            if (game == null || game.DayCycle == null)
            {
                return;
            }

            var open = game.DayCycle.Phase == BakeryDayPhase.Open;
            if (shutter != null && shutterSlats.Count > 0)
            {
                for (var index = 0; index < shutterSlats.Count; index++)
                {
                    var slat = shutterSlats[index];
                    var target = new Vector3(0f, -index * (open ? 0.04f : 0.32f), 0f);
                    slat.localPosition = Vector3.Lerp(slat.localPosition, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4.2f));
                }
            }

            var travelling = game.DayCycle.Phase == BakeryDayPhase.TravellingToMarket;
            if (bicycle != null) bicycle.gameObject.SetActive(game.CurrentSnapshot.BakeryLevel == 1 && !travelling);
            if (deliveryCar != null) deliveryCar.gameObject.SetActive(game.CurrentSnapshot.BakeryLevel >= 2 && !travelling);
            if (friend != null)
            {
                friend.localPosition = friendBase + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.035f);
                friend.localRotation = Quaternion.Euler(0f, 18f + Mathf.Sin(time * 0.43f) * 4f, 0f);
            }
        }
    }
}
