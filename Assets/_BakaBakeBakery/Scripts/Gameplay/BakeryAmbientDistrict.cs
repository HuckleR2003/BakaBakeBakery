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
        private readonly List<Transform> parkCrowns = new();
        private readonly List<Quaternion> parkCrownBases = new();
        private readonly List<Transform> skyClouds = new();
        private readonly List<Vector3> skyCloudBases = new();
        private readonly List<Transform> skyBirds = new();
        private readonly List<Transform> parkLeaves = new();
        private readonly List<Vector3> parkLeafBases = new();
        private readonly List<Transform> lampGlows = new();
        private readonly List<Light> lampLights = new();
        private readonly List<Transform> buntingFlags = new();
        private readonly List<Quaternion> buntingFlagBases = new();
        private readonly List<Transform> laundryCloths = new();
        private readonly List<Quaternion> laundryClothBases = new();
        private Transform catTail;
        private Quaternion catTailBase;
        private Light keyLight;
        private Color keyLightBase;
        private Light counterLight;
        private float counterLightBase;
        private BakeryGameController game;
        private Transform shutter;
        private Transform bicycle;
        private Transform deliveryCar;
        private Transform friend;
        private Transform friendLeftArm;
        private Transform friendRightArm;
        private Vector3 friendBase;
        private Quaternion friendLeftArmBase;
        private Quaternion friendRightArmBase;
        private float friendArrivalStartedAt;
        private bool friendArrivalInitialized;
        private bool playFriendArrival;
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
                else if (item.name.StartsWith("Park Crown")) parkCrowns.Add(item);
                else if (item.name == "Service Shutter") shutter = item;
                else if (item.name == "Morning Bicycle") bicycle = item;
                else if (item.name == "Old Delivery Car") deliveryCar = item;
                else if (item.name.StartsWith("Sky Cloud")) skyClouds.Add(item);
                else if (item.name.StartsWith("Sky Bird")) skyBirds.Add(item);
                else if (item.name.StartsWith("Park Leaf")) parkLeaves.Add(item);
                else if (item.name.StartsWith("Street Lamp Glow")) lampGlows.Add(item);
                else if (item.name.StartsWith("Bunting Flag")) buntingFlags.Add(item);
                else if (item.name.StartsWith("Laundry Cloth")) laundryCloths.Add(item);
                else if (item.name == "Cat Tail") catTail = item;
                else if (item.name == "Friend - Mila") friend = item;
                else if (item.name == "Mila Arm Left") friendLeftArm = item;
                else if (item.name == "Mila Arm Right") friendRightArm = item;
            }

            if (friend != null) friendBase = friend.localPosition;
            foreach (var crown in parkCrowns) parkCrownBases.Add(crown.localRotation);
            foreach (var cloud in skyClouds) skyCloudBases.Add(cloud.localPosition);
            foreach (var leaf in parkLeaves) parkLeafBases.Add(leaf.localPosition);
            foreach (var flag in buntingFlags) buntingFlagBases.Add(flag.localRotation);
            foreach (var cloth in laundryCloths) laundryClothBases.Add(cloth.localRotation);
            foreach (var glow in lampGlows)
            {
                var light = glow.GetComponentInChildren<Light>(true);
                if (light != null) lampLights.Add(light);
            }

            if (catTail != null) catTailBase = catTail.localRotation;
            if (friendLeftArm != null) friendLeftArmBase = friendLeftArm.localRotation;
            if (friendRightArm != null) friendRightArmBase = friendRightArm.localRotation;
            CacheMoodLights();
        }

        private void CacheMoodLights()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.name == "Late Afternoon Key")
                {
                    keyLight = light;
                    keyLightBase = light.color;
                }
                else if (light.name == "Counter Honey Light")
                {
                    counterLight = light;
                    counterLightBase = light.intensity;
                }
            }
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            AnimateWindows(time);
            AnimateSmoke(time);
            AnimateFlour(time);
            AnimatePark(time);
            AnimateSky(time);
            AnimateStreetLife(time);
            AnimateBakeryState(time);
            AnimateMood(deltaTime);
        }

        private void AnimateSky(float time)
        {
            var motionScale = BakaBakeBakery.Core.GameSettings.ReduceMotion ? 0.35f : 1f;
            for (var index = 0; index < skyClouds.Count; index++)
            {
                var basePosition = skyCloudBases[index];
                var drift = Mathf.Repeat(time * (0.22f + index % 3 * 0.05f) * motionScale + index * 5.7f, 34f) - 17f;
                skyClouds[index].localPosition = new Vector3(
                    basePosition.x + drift,
                    basePosition.y + Mathf.Sin(time * 0.24f + index) * 0.16f * motionScale,
                    basePosition.z);
            }

            for (var index = 0; index < skyBirds.Count; index++)
            {
                var bird = skyBirds[index];
                var cycle = Mathf.Repeat(time * 0.075f * motionScale + index * 0.37f, 1f);
                var glide = Mathf.Sin(cycle * Mathf.PI * 2f);
                bird.localPosition = new Vector3(
                    Mathf.Lerp(-14f, 14f, cycle),
                    8.2f + index * 1.05f + glide * 0.6f * motionScale,
                    11.5f + index * 3.1f);
                bird.localRotation = Quaternion.Euler(0f, 0f, glide * 9f * motionScale);
                var flap = 0.55f + Mathf.Abs(Mathf.Sin(time * 7.5f + index)) * 0.7f * motionScale;
                bird.localScale = new Vector3(1f, flap, 1f);
            }
        }

        private void AnimateStreetLife(float time)
        {
            var motionScale = BakaBakeBakery.Core.GameSettings.ReduceMotion ? 0.3f : 1f;
            for (var index = 0; index < parkLeaves.Count; index++)
            {
                var basePosition = parkLeafBases[index];
                var fall = Mathf.Repeat(time * 0.19f + index * 0.13f, 1f);
                parkLeaves[index].localPosition = new Vector3(
                    basePosition.x + Mathf.Sin((fall + index) * 6.1f) * 0.62f * motionScale,
                    Mathf.Lerp(basePosition.y, 0.24f, fall),
                    basePosition.z + Mathf.Cos((fall + index) * 4.3f) * 0.34f * motionScale);
                parkLeaves[index].localRotation = Quaternion.Euler(
                    fall * 320f * motionScale,
                    index * 37f,
                    Mathf.Sin(fall * 9f) * 26f * motionScale);
            }

            for (var index = 0; index < buntingFlags.Count; index++)
            {
                buntingFlags[index].localRotation = buntingFlagBases[index]
                    * Quaternion.Euler(Mathf.Sin(time * 1.9f + index * 0.62f) * 7.5f * motionScale, 0f, 0f);
            }

            for (var index = 0; index < laundryCloths.Count; index++)
            {
                laundryCloths[index].localRotation = laundryClothBases[index]
                    * Quaternion.Euler(Mathf.Sin(time * 1.35f + index * 0.9f) * 11f * motionScale, 0f, 0f);
            }

            if (catTail != null)
            {
                catTail.localRotation = catTailBase
                    * Quaternion.Euler(0f, Mathf.Sin(time * 1.7f) * 24f * motionScale, 0f);
            }
        }

        private void AnimateMood(float deltaTime)
        {
            var phase = game?.DayCycle?.Phase ?? BakeryDayPhase.MorningPreparation;
            var open = phase == BakeryDayPhase.Open;
            var evening = phase == BakeryDayPhase.DaySummary;
            var blend = 1f - Mathf.Exp(-deltaTime * 1.6f);

            if (keyLight != null)
            {
                var target = evening
                    ? keyLightBase * 0.72f + new Color(0.12f, 0.03f, 0f)
                    : open
                        ? keyLightBase
                        : keyLightBase * 0.86f + new Color(0f, 0.02f, 0.08f);
                keyLight.color = Color.Lerp(keyLight.color, target, blend);
            }

            if (counterLight != null)
            {
                var target = open ? counterLightBase : counterLightBase * 0.34f;
                counterLight.intensity = Mathf.Lerp(counterLight.intensity, target, blend);
            }

            // Lamps carry the street once the shutter is down and through the closing summary.
            var lampTarget = open ? 1.35f : 2.9f;
            for (var index = 0; index < lampLights.Count; index++)
            {
                var light = lampLights[index];
                if (light == null)
                {
                    continue;
                }

                var flicker = 1f + Mathf.Sin(Time.unscaledTime * 5.3f + index * 2.1f) * 0.035f;
                light.intensity = Mathf.Lerp(light.intensity, lampTarget * flicker, blend);
            }
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

        private void AnimatePark(float time)
        {
            var motionScale = BakaBakeBakery.Core.GameSettings.ReduceMotion ? 0.3f : 1f;
            for (var index = 0; index < parkCrowns.Count; index++)
            {
                var crown = parkCrowns[index];
                crown.localRotation = parkCrownBases[index]
                    * Quaternion.Euler(
                        Mathf.Sin(time * 0.42f + index * 0.7f) * 1.1f * motionScale,
                        0f,
                        Mathf.Sin(time * 0.36f + index * 0.43f) * 1.8f * motionScale);
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
                if (!friendArrivalInitialized && game.IsReady)
                {
                    friendArrivalInitialized = true;
                    playFriendArrival = game.TutorialStep == (int)BakeryTutorialStep.Welcome;
                    friendArrivalStartedAt = time;
                }

                var arrivalOffset = 0f;
                if (playFriendArrival)
                {
                    var progress = Mathf.Clamp01((time - friendArrivalStartedAt) / 2.4f);
                    progress = progress * progress * (3f - 2f * progress);
                    arrivalOffset = Mathf.Lerp(2.8f, 0f, progress);
                    if (progress >= 1f) playFriendArrival = false;
                }

                friend.localPosition = friendBase
                    + Vector3.right * arrivalOffset
                    + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.035f);
                friend.localRotation = Quaternion.Euler(0f, 18f + Mathf.Sin(time * 0.43f) * 4f, 0f);

                var greeting = game.TutorialStep <= (int)BakeryTutorialStep.VisitMarket;
                var armMotion = Mathf.Sin(time * (greeting ? 3.4f : 1.25f)) * (greeting ? 8f : 2.2f);
                if (friendLeftArm != null)
                {
                    friendLeftArm.localRotation = friendLeftArmBase * Quaternion.Euler(0f, 0f, -armMotion * 0.45f);
                }
                if (friendRightArm != null)
                {
                    friendRightArm.localRotation = friendRightArmBase * Quaternion.Euler(0f, 0f, armMotion);
                }
            }
        }
    }
}
