/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タッチまたはクリック位置に ParticleSystem のタッチエフェクトを再生する。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shiyuan.Foundation.Effects
{
    public sealed class TouchEffectPlayer : SingletonMonoBehaviour<TouchEffectPlayer>
    {
        private const string TouchEffectLayerName = "TouchEffect";
        private const string TouchEffectCameraName = "TouchEffectCamera";

        [SerializeField]
        private ParticleSystem touchEffectPrefab;

        [SerializeField]
        private Camera touchEffectCamera;

        [SerializeField]
        private int initialPoolSize = 6;

        [SerializeField]
        private int maxPoolSize = 16;

        [SerializeField]
        private float cameraDistance = 10f;

        private readonly List<ParticleSystem> particlePool = new List<ParticleSystem>();
        private Transform effectParent;
        private int nextReusableIndex;
        private int touchEffectLayer;
        private bool isInitialized;

        /// <summary>
        /// タッチエフェクト再生の初期化が完了しているかどうかを取得する。
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 親の AppRoot が常駐するため、この GameObject 自身は常駐化しない。
        /// </summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>
        /// シングルトンを登録し、タッチエフェクトを常駐させる。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return;
            }

            effectParent = transform;
            _ = InitializeAsync(destroyCancellationToken);
        }

        /// <summary>
        /// タッチエフェクト再生に必要な参照を内部解決し、プール生成の完了まで待機する。
        /// </summary>
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (isInitialized)
            {
                return;
            }

            while (touchEffectCamera == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ResolveTouchEffectCamera();
                if (touchEffectCamera != null)
                {
                    break;
                }

                await Task.Yield();
            }

            Initialize();
        }

        /// <summary>
        /// タッチエフェクト再生に必要な参照を確認し、プールを準備する。
        /// </summary>
        private void Initialize()
        {
            effectParent = effectParent != null ? effectParent : transform;

            touchEffectLayer = LayerMask.NameToLayer(TouchEffectLayerName);
            if (touchEffectLayer < 0)
            {
                isInitialized = false;
                throw new InvalidOperationException("TouchEffect レイヤーが設定されていません。");
            }

            if (touchEffectPrefab == null || touchEffectCamera == null)
            {
                isInitialized = false;
                throw new InvalidOperationException("TouchEffectPlayer の Prefab または Camera が未設定です。");
            }

            CreateInitialPool();
            isInitialized = true;
        }

        /// <summary>
        /// シーン内からタッチ専用カメラを検索して設定する。
        /// </summary>
        private void ResolveTouchEffectCamera()
        {
            if (touchEffectCamera != null)
            {
                return;
            }

            var cameraObject = GameObject.Find(TouchEffectCameraName);
            touchEffectCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        }

        /// <summary>
        /// 入力開始位置にタッチエフェクトを再生する。
        /// </summary>
        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            PlayTouchEffects();
            PlayMouseEffect();
        }

        /// <summary>
        /// タッチ入力の開始位置にエフェクトを再生する。
        /// </summary>
        private void PlayTouchEffects()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    PlayAtScreenPosition(touch.position.ReadValue());
                }
            }
        }

        /// <summary>
        /// エディタ確認用にマウスクリック位置へエフェクトを再生する。
        /// </summary>
        private void PlayMouseEffect()
        {
            var mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame != true)
            {
                return;
            }

            PlayAtScreenPosition(mouse.position.ReadValue());
        }

        /// <summary>
        /// 画面座標をタッチ専用カメラのワールド座標へ変換してエフェクトを再生する。
        /// </summary>
        private void PlayAtScreenPosition(Vector2 screenPosition)
        {
            var worldPosition = touchEffectCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, cameraDistance));
            var particle = GetAvailableParticle();

            particle.transform.position = worldPosition;
            particle.transform.rotation = touchEffectCamera.transform.rotation;
            particle.Clear(true);
            particle.Play(true);
        }

        /// <summary>
        /// 初期プール数までエフェクトインスタンスを生成する。
        /// </summary>
        private void CreateInitialPool()
        {
            if (particlePool.Count > 0)
            {
                return;
            }

            var poolSize = Mathf.Clamp(initialPoolSize, 1, maxPoolSize);
            for (var i = 0; i < poolSize; i++)
            {
                particlePool.Add(CreateParticle());
            }
        }

        /// <summary>
        /// 再生可能な ParticleSystem を取得する。
        /// </summary>
        private ParticleSystem GetAvailableParticle()
        {
            foreach (var particle in particlePool)
            {
                if (!particle.IsAlive(true))
                {
                    return particle;
                }
            }

            if (particlePool.Count < maxPoolSize)
            {
                var particle = CreateParticle();
                particlePool.Add(particle);
                return particle;
            }

            var reusableParticle = particlePool[nextReusableIndex];
            nextReusableIndex = (nextReusableIndex + 1) % particlePool.Count;
            return reusableParticle;
        }

        /// <summary>
        /// タッチエフェクト Prefab からプール用インスタンスを生成する。
        /// </summary>
        private ParticleSystem CreateParticle()
        {
            var particle = Instantiate(touchEffectPrefab, effectParent);
            particle.name = touchEffectPrefab.name;
            ConfigureParticle(particle);
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetLayerRecursively(particle.gameObject, touchEffectLayer);
            return particle;
        }

        /// <summary>
        /// タッチエフェクト用の短い発光パーティクル設定を適用する。
        /// </summary>
        private static void ConfigureParticle(ParticleSystem particle)
        {
            var main = particle.main;
            main.duration = 0.36f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.95f),
                new Color(0.35f, 0.85f, 1f, 0.75f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 28)
            });

            var shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.radiusThickness = 0f;
            shape.arc = 360f;

            DisableUnusedModules(particle);

            var colorOverLifetime = particle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.85f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.65f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particle.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.65f),
                    new Keyframe(0.35f, 1.25f),
                    new Keyframe(1f, 0f)));

            var renderer = particle.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = 32050;
        }

        /// <summary>
        /// タッチエフェクトに不要な ParticleSystem モジュールを無効化する。
        /// </summary>
        private static void DisableUnusedModules(ParticleSystem particle)
        {
            var velocityOverLifetime = particle.velocityOverLifetime;
            velocityOverLifetime.enabled = false;

            var limitVelocityOverLifetime = particle.limitVelocityOverLifetime;
            limitVelocityOverLifetime.enabled = false;

            var inheritVelocity = particle.inheritVelocity;
            inheritVelocity.enabled = false;

            var forceOverLifetime = particle.forceOverLifetime;
            forceOverLifetime.enabled = false;

            var colorBySpeed = particle.colorBySpeed;
            colorBySpeed.enabled = false;

            var sizeBySpeed = particle.sizeBySpeed;
            sizeBySpeed.enabled = false;

            var rotationOverLifetime = particle.rotationOverLifetime;
            rotationOverLifetime.enabled = false;

            var rotationBySpeed = particle.rotationBySpeed;
            rotationBySpeed.enabled = false;

            var externalForces = particle.externalForces;
            externalForces.enabled = false;

            var noise = particle.noise;
            noise.enabled = false;

            var collision = particle.collision;
            collision.enabled = false;

            var trigger = particle.trigger;
            trigger.enabled = false;

            var subEmitters = particle.subEmitters;
            subEmitters.enabled = false;

            var textureSheetAnimation = particle.textureSheetAnimation;
            textureSheetAnimation.enabled = false;

            var lights = particle.lights;
            lights.enabled = false;

            var trails = particle.trails;
            trails.enabled = false;

            var customData = particle.customData;
            customData.enabled = false;
        }

        /// <summary>
        /// 指定 GameObject と子階層へレイヤーを設定する。
        /// </summary>
        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
