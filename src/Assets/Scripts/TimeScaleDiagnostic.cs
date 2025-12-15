using UnityEngine;
using System;

public class TimeScaleDiagnostic : MonoBehaviour
{
    private string debugId;
    
    void Start()
    {

        debugId = System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ========== UNITY RUNTIME TIMESCALE CHECK ==========");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Debug ID: {debugId} (use this to filter logs)");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.timeScale at Start: {Time.timeScale}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.deltaTime: {Time.deltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.realtimeSinceStartup: {Time.realtimeSinceStartup}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] =================================================");
    }
    
    void Update()
    {

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Frame {Time.frameCount}: Time.timeScale = {Time.timeScale}, Time.deltaTime = {Time.deltaTime:F6}, Time.realtimeSinceStartup = {Time.realtimeSinceStartup:F2}");
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] OnApplicationPause: {pauseStatus}, Time.timeScale = {Time.timeScale}");
    }
}
