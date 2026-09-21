using System;
using UnityEngine;
using UnityEngine.InputSystem;

// MVP: tieni premuto SPAZIO per simulare il respiro.
// Il gauge sale mentre tieni premuto, scende quando lasci.
// Quando rimane sopra la soglia per stableDuration secondi, lancia OnStableReached.
public class BreathingMechanic : MonoBehaviour
{
    [SerializeField] float fillSpeed      = 0.5f;
    [SerializeField] float drainSpeed     = 0.7f;
    [SerializeField] float stableThreshold = 0.7f;
    [SerializeField] float stableDuration  = 3f;
    [SerializeField] EngineGauge engineGauge;

    public event Action OnStableReached;

    float power;
    float stableTimer;
    bool active;
    bool eventFired;

    public void Enable()
    {
        active     = true;
        eventFired = false;
        stableTimer = 0f;
    }

    public void Disable()
    {
        active = false;
    }

    void Update()
    {
        if (!active) return;

        bool pressing = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        power = pressing
            ? Mathf.MoveTowards(power, 1f, fillSpeed  * Time.deltaTime)
            : Mathf.MoveTowards(power, 0f, drainSpeed * Time.deltaTime);

        engineGauge?.SetPower(power);

        if (power >= stableThreshold)
        {
            stableTimer += Time.deltaTime;
            if (!eventFired && stableTimer >= stableDuration)
            {
                eventFired = true;
                OnStableReached?.Invoke();
            }
        }
        else
        {
            stableTimer = 0f;
        }
    }
}
