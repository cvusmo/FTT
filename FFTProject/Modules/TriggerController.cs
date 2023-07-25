﻿using BepInEx.Logging;
using FFT;
using KSP.Animation;
using KSP.Game;
using KSP.Messages.PropertyWatchers;
using KSP.Sim.impl;
using KSP.Sim.ResourceSystem;
using KSP.Sim.State;
using SpaceWarp.API.Game;
using UnityEngine;
using static KSP.Api.UIDataPropertyStrings.View;

public class TriggerController : MonoBehaviour
{
    private TriggerVFXFromAnimation _triggerVFX;
    private Animator _animator;
    private ParticleSystem _particleSystem;
    private IsActiveVessel _isActiveVessel;
    private VesselComponent _vesselComponent;
    [SerializeField]
    private GameObject CoolingVFX;
    private bool _wasActive;
    public bool _isActive;
    internal new static ManualLogSource Logger { get; set; }

    public float FuelLevel
    {
        get
        {
            _vesselComponent.RefreshFuelPercentages();
            double fuelPercentage = _vesselComponent.StageFuelPercentage;

            if (fuelPercentage < 0)
            {
                Logger.LogError("Out of Fuel." + fuelPercentage);
                return 0f;
            }
            else
            {
                float level = (float)fuelPercentage;
                _animator.SetFloat("FuelLevel", level);
                Logger.LogInfo("Fuel level: " + level);
                return level;
            }
        }
    }

    private void Start()
    {
        Logger = FFTPlugin.Logger;
        Logger.LogInfo("Attached to GameObject: " + gameObject.name);

        _triggerVFX = GetComponent<TriggerVFXFromAnimation>();
        if (_triggerVFX == null)
        {
            Logger.LogError("TriggerVFXFromAnimation not found in same GameObject");
            return;
        }

        _particleSystem = CoolingVFX.GetComponent<ParticleSystem>();
        if (_particleSystem == null)
        {
            Logger.LogError("ParticleSystem not found in same GameObject");
            return;
        }

        _animator = GetComponentInParent<Animator>();
        if (_animator == null)
        {
            Logger.LogError("Animator not found in parent GameObject");
            return;
        }

        _isActiveVessel = new IsActiveVessel();
        _vesselComponent = new VesselComponent();

        Logger.LogInfo("TriggerController has started.");
    }
    public void EnableEmission()
    {
        var emission = _particleSystem.emission;
        emission.enabled = true;
    }
    public void DisableEmission()
    {
        var emission = _particleSystem.emission;
        emission.enabled = false;
    }
    public void StartParticleSystem()
    {
        _particleSystem.Play();
    }
    public void StopParticleSystem()
    {
        _particleSystem.Stop();
    }
    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if (_isActive)
            {
                if (!_wasActive && FuelLevelExceedsThreshold())
                {
                    EnableEmission();
                    _triggerVFX.enabled = true;
                    StartParticleSystem();
                    _animator.Play("CoolingVFX_ON");
                    Logger.LogInfo("CoolingVFX_ON: ");
                }
            }
            else
            {
                DisableEmission();
                _triggerVFX.enabled = false;
            }

            _wasActive = _isActive;
        }
    }
    private void FixedUpdate()
    {
        _vesselComponent = Vehicle.ActiveSimVessel;

        Logger.LogInfo("ActiveSimVessel: " + Vehicle.ActiveSimVessel);

        _animator.SetBool("IsActive", _isActive);
        IsActive = _isActiveVessel.GetValueBool();

        if (_wasActive)
        {
            DisableCoolingEffects();
        }
    }
    private bool FuelLevelExceedsThreshold()
    {
        return FuelLevel > 0.8f;
    }

    private void HandleActiveVessel(IResourceContainer __fuelContainer)
    {
        if (FuelLevelExceedsThreshold() && !_animator.GetCurrentAnimatorStateInfo(0).IsName("CoolingVFX_LOOP"))
        {
            _triggerVFX.enabled = true;
            StartParticleSystem();
            _animator.Play("CoolingVFX_LOOP");
            Logger.LogInfo("CoolingVFX_LOOP: ");
            Logger.LogInfo("__fuelContainer: " + __fuelContainer);
        }
        else if (!FuelLevelExceedsThreshold() && _animator.GetCurrentAnimatorStateInfo(0).IsName("CoolingVFX_LOOP"))
        {
            DisableCoolingEffects();
        }
    }
    private void DisableCoolingEffects()
    {
        _animator.Play("CoolingVFX_OFF");
        _triggerVFX.enabled = true;
        StopParticleSystem();
        DisableEmission();
    }
}
