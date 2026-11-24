using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using InputGyroscope = UnityEngine.InputSystem.Gyroscope;

// Integrates hardware angular-velocity samples around gravity, independent of screen rotation.
public sealed class RunningGyroSource : MonoBehaviour
{
    [SerializeField] private bool invertYaw;
    private InputGyroscope gyro;
    private GravitySensor gravity;
    private Accelerometer accelerometer;
    private bool ownedGyro, ownedGravity, ownedAcceleration, suspended;
    private double lastEventTime = -1, lastSampleRealtime = -100;
    private Vector3 filteredGravity;
    public double YawDegrees { get; private set; }
    public float RateDegreesPerSecond { get; private set; }
    public int Discontinuities { get; private set; }
    public bool HasHardware => gyro != null && (gravity != null || accelerometer != null);
    public bool IsReady => HasHardware && !suspended && lastEventTime >= 0 && Time.realtimeSinceStartupAsDouble - lastSampleRealtime < 0.5;
    public string Status => !HasHardware ? "Este teléfono no ofrece giroscopio y referencia de gravedad." : !IsReady ? "Esperando datos del giroscopio…" : "Giroscopio listo";
    private void OnEnable()
    {
        gyro = InputGyroscope.current; gravity = GravitySensor.current; accelerometer = gravity == null ? Accelerometer.current : null;
        if (gyro != null) { ownedGyro = !gyro.enabled; if (ownedGyro) InputSystem.EnableDevice(gyro); try { gyro.samplingFrequency = 60f; } catch (System.Exception error) { Debug.LogWarning("Se usará la frecuencia disponible del giroscopio: " + error.Message); } }
        if (gravity != null) { ownedGravity = !gravity.enabled; if (ownedGravity) InputSystem.EnableDevice(gravity); }
        if (accelerometer != null) { ownedAcceleration = !accelerometer.enabled; if (ownedAcceleration) InputSystem.EnableDevice(accelerometer); }
        lastEventTime = -1; lastSampleRealtime = -100;
        InputSystem.onEvent += OnSensorEvent;
    }
    private void OnDisable()
    {
        InputSystem.onEvent -= OnSensorEvent;
        if (ownedGyro && gyro != null && gyro.added) InputSystem.DisableDevice(gyro);
        if (ownedGravity && gravity != null && gravity.added) InputSystem.DisableDevice(gravity);
        if (ownedAcceleration && accelerometer != null && accelerometer.added) InputSystem.DisableDevice(accelerometer);
        lastEventTime = -1; Discontinuities++;
    }
    private void OnApplicationPause(bool pause)
    { suspended = pause; lastEventTime = -1; lastSampleRealtime = -100; Discontinuities++; }
    private void OnSensorEvent(InputEventPtr evt, InputDevice device)
    {
        if (suspended || device != gyro || !(evt.IsA<StateEvent>() || evt.IsA<DeltaStateEvent>())) return;
        if (!gyro.angularVelocity.ReadValueFromEvent(evt, out Vector3 velocity)) return;
        Vector3 down = gravity != null ? gravity.gravity.ReadValue() : accelerometer != null ? accelerometer.acceleration.ReadValue() : Vector3.zero;
        if (!Finite(down.x) || !Finite(down.y) || !Finite(down.z) || down.sqrMagnitude < 0.1f || !Finite(velocity.x) || !Finite(velocity.y) || !Finite(velocity.z)) return;
        filteredGravity = gravity != null || filteredGravity.sqrMagnitude < 0.1f ? down : Vector3.Lerp(filteredGravity, down, 0.03f);
        float rate = Vector3.Dot(velocity, filteredGravity.normalized) * Mathf.Rad2Deg * (invertYaw ? -1f : 1f);
        RateDegreesPerSecond = rate;
        double delta = lastEventTime < 0 ? 0 : evt.time - lastEventTime;
        lastEventTime = evt.time; lastSampleRealtime = Time.realtimeSinceStartupAsDouble;
        if (delta <= 0) return;
        if (delta > 0.25) { Discontinuities++; return; }
        YawDegrees += rate * delta;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
