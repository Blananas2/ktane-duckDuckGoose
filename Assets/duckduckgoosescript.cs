﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class duckDuckGooseScript : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMNeedyModule module;

    public KMSelectable[] buttons;
    public Texture[] duckImages;
    public Texture[] gooseImages;
    public Texture[] neitherImages;
    public Renderer screen;

    private bool active;
    private bool bombSolved;
    private int solution;

    private static int moduleIdCounter = 1;
    private int moduleId;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        module.OnNeedyActivation += OnNeedyActivation;
        module.OnNeedyDeactivation += OnNeedyDeactivation;
        module.OnTimerExpired += OnTimerExpired;
        bomb.OnBombExploded += delegate () { bombSolved = true; };
        bomb.OnBombSolved += delegate () { bombSolved = true; };
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
    }

    void Start()
    {
        Debug.LogFormat("[Duck, Duck, Goose #{0}] Needy initiated.", moduleId);
        module.SetResetDelayTime(30f, 45f);
    }

    protected void OnNeedyActivation()
    {
        active = true;
        solution = rnd.Range(0, 3);
        screen.material.color = Color.white;
        switch (solution)
        {
            case 0:
                screen.material.mainTexture = duckImages.PickRandom();
                break;
            case 1:
                screen.material.mainTexture = neitherImages.PickRandom();
                break;
            case 2:
                screen.material.mainTexture = gooseImages.PickRandom();
                break;
        }
        var animals = new string[] { "Duck", "Neither", "Goose" };
        Debug.LogFormat("[Duck, Duck, Goose #{0}] Needy activated. Displayed animal: {1}", moduleId, animals[solution]);
    }

    protected void OnNeedyDeactivation()
    {
        active = false;
    }

    protected void OnTimerExpired()
    {
        if (active)
        {
            Debug.LogFormat("[Duck, Duck, Goose #{0}] An option was not selected in time, strike!", moduleId);
            module.OnStrike();
            OnNeedyDeactivation();
        }
    }

    void ButtonPress(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        if (!active)
            return;
        var ix = Array.IndexOf(buttons, button);
        Debug.LogFormat("[Duck, Duck, Goose #{0}] Selected: {1}", moduleId, button.GetComponentInChildren<TextMesh>().text);
        audio.PlaySoundAtTransform(button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant(), transform);
        if (ix != solution)
        {
            Debug.LogFormat("[Duck, Duck, Goose #{0}] That was not correct. Strike!", moduleId);
            module.HandleStrike();
            module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[Duck, Duck, Goose #{0}] That was correct. Module passed.", moduleId);
            module.HandlePass();
        }
        OnNeedyDeactivation();
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <duck/neither/goose> [Presses the respective button.]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (!new string[] { "duck", "neither", "goose" }.Contains(command.ToLowerInvariant()))
            yield break;
        yield return null;
        switch (command.ToLowerInvariant())
        {
            case "duck":
                buttons[0].OnInteract();
                break;
            case "neither":
                buttons[1].OnInteract();
                break;
            case "goose":
                buttons[2].OnInteract();
                break;
            default:
                yield break;
        }
    }

    void TwitchHandleForcedSolve()
    {
        // The code is done in a coroutine instead of here so that if the solvebomb command was executed this will just input the number right when it activates and it wont wait for its turn in the queue
        StartCoroutine(HandleSolve());
    }

    IEnumerator HandleSolve()
    {
        while (!bombSolved)
        {
            while (!active) { yield return new WaitForSeconds(0.1f); }
            buttons[solution].OnInteract();
        }
    }
}
