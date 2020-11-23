﻿using BepInEx;
using BepInEx.Configuration;
using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Karenia.GetTapped.Core;

namespace Karenia.GetTapped.Com3d2
{
    [BepInPlugin(id, projectName, version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string id = "cc.karenia.gettapped.com3d2";
        public const string projectName = "GetTapped.COM3D2";
        public const string version = "0.1.0";

        public Plugin()
        {
            Instance = this;
            Config = new PluginConfig()
            {
                DefaultRotationSensitivity = 0.007f,
                DefaultTranslationSensitivity = -0.01f
            };
            Config.BindConfig(base.Config);
            LongPressThreshold = base.Config.Bind("default", nameof(LongPressThreshold), 0.6f, "Press longer than this value (in seconds) will be viewed as a long press");
            LongPressThreshold.SettingChanged += (sender, _) => { longPressDetector.LongPressThreshold = ((ConfigEntry<float>)sender).Value; };

            Logger = BepInEx.Logging.Logger.CreateLogSource("GetTapped");
            var harmony = new Harmony(id);
            Core = new PluginCore();

            harmony.PatchAll(typeof(Hook));
        }

        public void LateUpdate()
        {
            longPressDetector.LateUpdate();
        }

        public static Plugin Instance { get; private set; }
        public new PluginConfig Config { get; set; }
        public new BepInEx.Logging.ManualLogSource Logger { get; private set; }
        public IGetTappedPlugin Core { get; private set; }

        public LongPressDetector longPressDetector { get; } = new LongPressDetector();

        public ConfigEntry<bool> PluginEnabled { get => Config.PluginEnabled; }
        public ConfigEntry<bool> SingleTapTranslate { get => Config.SingleTapTranslate; }
        public ConfigEntry<float> RotationSensitivity { get => Config.RotationSensitivity; }
        public ConfigEntry<float> TranslationSensitivity { get => Config.TranslationSensitivity; }
        public ConfigEntry<float> ZoomSensitivity { get => Config.ZoomSensitivity; }
        public ConfigEntry<float> LongPressThreshold { get; }
    }

    public class LongPressDetector
    {
        public class TouchState
        {
            public bool hasMoved = false;
            public float activeTime = 0f;
            public event Action<TouchState> onLongPressCallback = null;
            public event Action<TouchState> onShortPressCallback = null;
            public event Action<TouchState> onPressCancelCallback = null;

            public void OnPressCancel()
            {
                if (onPressCancelCallback != null)
                    onPressCancelCallback.Invoke(this);
            }

            public void OnFinish(float longPressThreshold)
            {
                if (activeTime >= longPressThreshold)
                {
                    if (onLongPressCallback != null)
                        onLongPressCallback.Invoke(this);
                }
                else
                {
                    if (onShortPressCallback != null)
                        onShortPressCallback.Invoke(this);
                }
            }
        }

        public enum EventState
        {
            LongPress, ShortPress, PressCancel
        }

        private readonly Dictionary<int, TouchState> touchStates = new Dictionary<int, TouchState>();
        public float LongPressThreshold { get; set; } = 2f;

        public void LateUpdate()
        {
            var touchCount = Input.touchCount;
            for (var i = 0; i < touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                int fingerId = touch.fingerId;
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStates.Add(fingerId, new TouchState());
                        break;
                    case TouchPhase.Stationary:
                        touchStates[fingerId].activeTime += touch.deltaTime;
                        break;
                    case TouchPhase.Moved:
                        {
                            TouchState touchState = touchStates[fingerId];
                            touchState.hasMoved = true;
                            touchState.activeTime += touch.deltaTime;
                            touchState.OnPressCancel();
                        }
                        break;
                    case TouchPhase.Canceled:
                    case TouchPhase.Ended:
                        {
                            var touchState = touchStates[fingerId];
                            touchState.OnFinish(LongPressThreshold);
                            touchStates.Remove(fingerId);
                        }
                        break;
                }
            }
        }

        public void SetCallback(int fingerId, EventState eventState, Action<TouchState> callback)
        {
            if (touchStates.TryGetValue(fingerId, out var state))
            {
                switch (eventState)
                {
                    case EventState.LongPress:
                        state.onLongPressCallback += callback;
                        break;
                    case EventState.PressCancel:
                        state.onPressCancelCallback += callback;
                        break;
                    case EventState.ShortPress:
                        state.onShortPressCallback += callback;
                        break;
                }
            }
        }

        public TouchState GetTouchTime(int touchId)
        {
            if (touchStates.TryGetValue(touchId, out var state))
            {
                return state;
            }
            else
            {
                return null;
            }
        }
    }

    public static class Hook
    {
        /// <summary>
        /// This method calculates the <b>rotation</b> update of the camera.
        /// </summary>
        /// <param name="___xVelocity"></param>
        /// <param name="___yVelocity"></param>
        /// <param name="___zoomVelocity"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UltimateOrbitCamera), "Update")]
        public static bool HookCameraRotation(
            UltimateOrbitCamera __instance,
            ref float ___xVelocity,
            ref float ___yVelocity,
            ref float ___zoomVelocity,
            ref Vector3 ___mVelocity)
        {
            var plugin = Plugin.Instance;
            if (!plugin.Config.PluginEnabled.Value) return true;

            var movement = plugin.Core.GetCameraMovement(plugin.SingleTapTranslate.Value);

            if (__instance.mouseControl)
            {
                // flip x direction because it's like that
                ___xVelocity -= movement.ScreenSpaceRotation.x * plugin.RotationSensitivity.Value;
                ___yVelocity += movement.ScreenSpaceRotation.y * plugin.RotationSensitivity.Value;
                ___zoomVelocity += -Mathf.Log(movement.Zoom) * plugin.ZoomSensitivity.Value;

                var tranform = __instance.transform;
                ___mVelocity += (
                        tranform.right * movement.ScreenSpaceTranslation.x +
                        tranform.up * movement.ScreenSpaceTranslation.y
                    ) * plugin.TranslationSensitivity.Value;
            }

            return true;
        }

        /// <summary>
        /// Recreates the button triggering method but being touchscreen aware
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPostfix, HarmonyPatch(typeof(SaveAndLoadMgr), "ClickSaveOrLoadData")]
        public static void FixUiButtonClick(SaveAndLoadMgr __instance, string ___currentActiveData, SaveAndLoadCtrl ___m_ctrl, SaveAndLoadMgr.ViewType ___currentView)
        {
            var name = UIButton.current.name;
            if (___currentActiveData == name)
            {
                return;
            }

            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                LongPressDetector longPressDetector = Plugin.Instance.longPressDetector;
                longPressDetector.SetCallback(touch.fingerId, LongPressDetector.EventState.LongPress, (_) =>
                {
                    if (___m_ctrl.ExistData(name))
                    {
                        ___m_ctrl.DeleteSaveOrLoadData(name);
                    }
                });
                longPressDetector.SetCallback(touch.fingerId, LongPressDetector.EventState.ShortPress, (_) =>
                {
                    __instance.SetCurrentActiveData(name);
                    ___m_ctrl.SaveAndLoad(___currentView, name);
                });
            }
        }
    }
}
