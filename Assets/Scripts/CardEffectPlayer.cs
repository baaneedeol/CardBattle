
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardEffectPlayer : MonoBehaviour
{
    [Header("One-Shot Particle Prefabs")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject drainEffect;

    [Header("Persistent Shield Prefab")]
    [SerializeField] private GameObject shieldEffect;

    [Header("Effect Locations")]
    [SerializeField] private Transform playerEffectPoint;
    [SerializeField] private Transform opponentEffectPoint;

    [Header("Effect Scaling")]
    [SerializeField] private float attackScale = 0.15f;
    [SerializeField] private float healScale = 0.15f;
    [SerializeField] private float drainScale = 0.15f;
    [SerializeField] private float shieldScale = 0.15f;

    [Header("Shield Position")]
    [SerializeField] private Vector3 shieldLocalOffset =
        new Vector3(0f, 0f, -0.05f);

    [Header("Temporary Effect Position Adjustments")]
    [SerializeField] private Vector3 attackPositionOffset =
        new Vector3(0f, -0.2f, 0f);

    [SerializeField] private Vector3 healPositionOffset =
        new Vector3(0f, -0.2f, 0f);

    [Header("Temporary Effect Settings")]
    [SerializeField] private float minimumEffectDuration = 1f;
    [SerializeField] private float cleanupBuffer = 1f;

    private GameObject playerShieldInstance;
    private GameObject opponentShieldInstance;

    private class EffectRequest
    {
        public GameObject prefab;
        public Transform target;
        public float scale;
        public Vector3 positionOffset;
    }

    private readonly Queue<EffectRequest> effectQueue =
        new Queue<EffectRequest>();

    private readonly List<GameObject> activeEffects =
        new List<GameObject>();

    private Coroutine effectQueueCoroutine;

    private int totalEffectsPlayed = 0;

    // ----------------------------------
    // TEST DIAGNOSTICS
    // ----------------------------------

    public int QueuedEffectCount => effectQueue.Count;

    public int ActiveTemporaryEffectCount => activeEffects.Count;

    public bool IsPlayingQueuedEffect =>
        effectQueueCoroutine != null;

    public int TotalEffectsPlayed => totalEffectsPlayed;

    // ----------------------------------
    // ATTACK EFFECT
    // ----------------------------------

    public void PlayAttack(bool playerIsAttacking)
    {
        Transform target = playerIsAttacking
            ? opponentEffectPoint
            : playerEffectPoint;

        QueueEffect(
            attackEffect,
            target,
            attackScale,
            attackPositionOffset
        );
    }

    // ----------------------------------
    // HEAL EFFECT
    // ----------------------------------

    public void PlayHeal(bool playerIsHealing)
    {
        Transform target = playerIsHealing
            ? playerEffectPoint
            : opponentEffectPoint;

        QueueEffect(
            healEffect,
            target,
            healScale,
            healPositionOffset
        );
    }

    // ----------------------------------
    // DRAIN EFFECT
    // ----------------------------------

    public void PlayDrain(bool playerIsDraining)
    {
        Transform target = playerIsDraining
            ? opponentEffectPoint
            : playerEffectPoint;

        QueueEffect(
            drainEffect,
            target,
            drainScale,
            Vector3.zero
        );
    }

    // ----------------------------------
    // QUEUE TEMPORARY EFFECTS
    // ----------------------------------

    private void QueueEffect(
        GameObject prefab,
        Transform target,
        float scale,
        Vector3 positionOffset
    )
    {
        if (prefab == null || target == null)
        {
            Debug.LogWarning(
                "CardEffectPlayer: Particle prefab or effect point is missing."
            );

            return;
        }

        effectQueue.Enqueue(new EffectRequest
        {
            prefab = prefab,
            target = target,
            scale = scale,
            positionOffset = positionOffset
        });

        if (effectQueueCoroutine == null)
        {
            effectQueueCoroutine =
                StartCoroutine(PlayEffectQueue());
        }
    }

    private IEnumerator PlayEffectQueue()
    {
        while (effectQueue.Count > 0)
        {
            EffectRequest request = effectQueue.Dequeue();

            if (request == null ||
                request.prefab == null ||
                request.target == null)
            {
                continue;
            }

            float duration = PlayTemporaryEffect(request);

            yield return new WaitForSeconds(duration);
        }

        effectQueueCoroutine = null;
    }

    private float PlayTemporaryEffect(EffectRequest request)
    {
        Vector3 spawnPosition =
            request.target.position +
            request.target.TransformDirection(request.positionOffset);

        GameObject instance = Instantiate(
            request.prefab,
            spawnPosition,
            request.target.rotation
        );

        instance.transform.localScale =
            Vector3.one * request.scale;

        activeEffects.Add(instance);

        totalEffectsPlayed++;

        PlayParticles(instance);

        ParticleSystem[] systems =
            instance.GetComponentsInChildren<ParticleSystem>(true);

        float longestLifetime = 0f;

        foreach (ParticleSystem system in systems)
        {
            var main = system.main;

            float particleLifetime =
                main.startLifetime.constantMax;

            float totalLifetime =
                main.duration + particleLifetime;

            longestLifetime = Mathf.Max(
                longestLifetime,
                totalLifetime
            );
        }

        float duration = Mathf.Max(
            longestLifetime,
            minimumEffectDuration
        ) + cleanupBuffer;

        Debug.Log(
            $"Playing queued effect: {instance.name}. " +
            $"Duration: {duration}s"
        );

        StartCoroutine(
            DestroyTemporaryEffect(instance, duration)
        );

        return duration;
    }

    private IEnumerator DestroyTemporaryEffect(
        GameObject instance,
        float delay
    )
    {
        yield return new WaitForSeconds(delay);

        if (instance != null)
        {
            activeEffects.Remove(instance);
            Destroy(instance);
        }
    }

    // ----------------------------------
    // PERSISTENT SHIELD EFFECT
    // ----------------------------------

    public void SetShield(bool playerHasShield, bool enabled)
    {
        Transform shieldTarget = playerHasShield
            ? playerEffectPoint
            : opponentEffectPoint;

        GameObject instance = playerHasShield
            ? playerShieldInstance
            : opponentShieldInstance;

        if (!enabled)
        {
            if (instance != null)
                Destroy(instance);

            if (playerHasShield)
                playerShieldInstance = null;
            else
                opponentShieldInstance = null;

            Debug.Log(
                $"{(playerHasShield ? "Player" : "AI")} shield removed."
            );

            return;
        }

        if (shieldTarget == null || shieldEffect == null)
        {
            Debug.LogError(
                "CardEffectPlayer: Shield prefab or effect point is missing."
            );
            return;
        }

        if (instance != null)
        {
            instance.transform.SetParent(shieldTarget, false);
            instance.transform.localPosition = shieldLocalOffset;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * shieldScale;

            if (!instance.activeSelf)
                instance.SetActive(true);

            return;
        }

        instance = Instantiate(shieldEffect, shieldTarget, false);

        instance.transform.localPosition = shieldLocalOffset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * shieldScale;

        instance.SetActive(true);

        if (playerHasShield)
            playerShieldInstance = instance;
        else
            opponentShieldInstance = instance;

        PlayPersistentShieldParticles(instance);

        Debug.Log(
            $"Created {(playerHasShield ? "PLAYER" : "AI")} shield."
        );
    }


    private void PlayPersistentShieldParticles(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(true);

        ParticleSystem[] systems =
            instance.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem system in systems)
        {
            system.gameObject.SetActive(true);

            var main = system.main;
            main.loop = true;

            system.Clear(true);
            system.Play(true);
        }
    }

    // ----------------------------------
    // CLEAR SHIELDS
    // ----------------------------------

    public void ClearAllShields()
    {
        SetShield(true, false);
        SetShield(false, false);
    }

    public void SyncShieldVisuals(int playerShield, int opponentShield)
    {
        SetShield(true, playerShield > 0);
        SetShield(false, opponentShield > 0);
    }

    // ----------------------------------
    // GENERAL PARTICLE PLAYBACK
    // ----------------------------------

    private void PlayParticles(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(true);

        ParticleSystem[] systems =
            instance.GetComponentsInChildren<ParticleSystem>(true);

        if (systems.Length == 0)
        {
            Debug.LogWarning(
                $"CardEffectPlayer: No ParticleSystem found under {instance.name}."
            );

            return;
        }

        foreach (ParticleSystem system in systems)
        {
            system.gameObject.SetActive(true);

            system.Clear(true);
            system.Play(true);
        }
    }

    // ----------------------------------
    // TEMPORARY EFFECT CLEANUP
    // ----------------------------------

    public void ClearEffect()
    {
        if (effectQueueCoroutine != null)
        {
            StopCoroutine(effectQueueCoroutine);
            effectQueueCoroutine = null;
        }

        effectQueue.Clear();

        foreach (GameObject effect in activeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }

        activeEffects.Clear();

        totalEffectsPlayed = 0;

        Debug.Log(
            "Temporary particle effects and queue cleared."
        );
    }

    // ----------------------------------
    // SHIELD REFERENCES
    // ----------------------------------

    public GameObject GetPlayerShield()
    {
        return playerShieldInstance;
    }

    public GameObject GetOpponentShield()
    {
        return opponentShieldInstance;
    }
}
