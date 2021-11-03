using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class melodyMemory : MonoBehaviour
{
    public new KMAudio audio;
    private KMAudio.KMAudioRef audioRef;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] keys;
    public KMSelectable playButton;
    public KMSelectable recordButton;
    public Renderer[] leds;
    public Color[] colors;
    public Color blankColor;
    public Color lightGray;
    public TextMesh colorblindText;

    private keyInfo[][] keysInfo = new keyInfo[5][];
    private List<keyInfo> pressedKeys = new List<keyInfo>();
    private int stage;
    private int solution;

    private bool recording;
    private bool cantPress;
    private bool firstTime = true;

    private static readonly string table = "ACBDABCDBCADADCBCDABBADCBDACDBACCDBABDCADCBADBCA";
    private static readonly string[] instrumentNames = new string[12] { "accordion", "acoustic guitar", "cello", "electric guitar", "french horn", "organ", "piano", "sitar", "trumpet", "violin", "voice", "xylophone" };
    private static readonly string[] colorNames = new string[4] { "red", "yellow", "green", "blue" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable key in keys)
            key.OnInteract += delegate () { PressKey(key); return false; };
        playButton.OnInteract += delegate () { PressPlayButton(); return false; };
        recordButton.OnInteract += delegate () { PressRecordButton(); return false; };
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    void Start()
    {
        for (int i = 0; i < 5; i++)
            keysInfo[i] = new keyInfo[5];
        GenerateStage();
    }

    void GenerateStage()
    {
        recordButton.GetComponent<Renderer>().material.color = lightGray;
        var letter = '0';
        if (stage != 5)
        {
            var keyColors = Enumerable.Range(0, 4).ToList().Shuffle().ToArray();
            var keyPitches = Enumerable.Range(0, 4).ToList().Shuffle().ToArray();
            var extraPitch = rnd.Range(0, 4);
            var sharedKey = Array.IndexOf(keyPitches, keyPitches.First(x => x == extraPitch));
            var keyInstruments = Enumerable.Range(0, 12).ToList().Shuffle().Take(5).ToArray();
            for (int i = 0; i < 5; i++)
                keysInfo[stage][i] = new keyInfo(i, i == 4 ? 0 : keyColors[i], i == 4 ? extraPitch : keyPitches[i], keyInstruments[i]);
            letter = table[(keysInfo[stage][4].instrument * 4) + keyColors[sharedKey]];
            Debug.LogFormat("[Melody Memory #{0}] Stage {1}:", moduleId, stage + 1);
            Debug.LogFormat("[Melody Memory #{0}] Colors: {1}", moduleId, keysInfo[stage].Take(4).Select(x => colorNames[x.color]).Join(", "));
            Debug.LogFormat("[Melody Memory #{0}] Pitches: {1}", moduleId, keysInfo[stage].Take(4).Select(x => x.pitch + 1).Join(", "));
            Debug.LogFormat("[Melody Memory #{0}] Instruments: {1}", moduleId, keysInfo[stage].Take(4).Select(x => instrumentNames[x.instrument]).Join(", "));
            Debug.LogFormat("[Melody Memory #{0}] The play button plays the note shared by button {1} ({2}) with the instrument {3}.", moduleId, sharedKey + 1, colorNames[keyColors[sharedKey]], instrumentNames[keysInfo[stage][4].instrument]);
            Debug.LogFormat("[Melody Memory #{0}] The letter from the table is {1}.", moduleId, letter);
            colorblindText.text = keysInfo[stage].Take(4).Select(x => "RYGB"[x.color]).Join();
        }
        StartCoroutine(ColorKeys());
        switch (stage)
        {
            case 0:
                switch (letter)
                {
                    case 'A':
                        solution = FindColorPitch(keysInfo[0], 0, true);
                        break;
                    case 'B':
                        solution = FindColorPitch(keysInfo[0], 2, false);
                        break;
                    case 'C':
                        solution = keysInfo[0].Take(4).OrderBy(x => x.instrument).ElementAt(2).position;
                        break;
                    case 'D':
                        solution = 3;
                        break;
                }
                break;
            case 1:
                switch (letter)
                {
                    case 'A':
                        solution = FindColorPitch(keysInfo[0], 2, true);
                        break;
                    case 'B':
                        solution = FindColorPitch(keysInfo[1], keysInfo[0].Select(x => x.color).ToArray()[FindColorPitch(keysInfo[0], 0, false)], true);
                        break;
                    case 'C':
                        var differences = keysInfo[stage].Take(4).Select(x => Math.Abs(x.instrument - pressedKeys[0].instrument)).ToArray();
                        solution = Array.IndexOf(differences, differences.Min());
                        break;
                    case 'D':
                        solution = FindColorPitch(keysInfo[1], keysInfo[0][2].color, true);
                        break;
                }
                break;
            case 2:
                switch (letter)
                {
                    case 'A':
                        solution = Array.IndexOf(keysInfo[2].Select(x => x.pitch).ToArray(), keysInfo[1].Select(x => x.pitch).ToArray()[FindColorPitch(keysInfo[1], 3, true)]);
                        break;
                    case 'B':
                        solution = FindColorPitch(keysInfo[2], pressedKeys[1].pitch, false);
                        break;
                    case 'C':
                        var differences = keysInfo[stage].Take(4).Select(x => Math.Abs(x.instrument - keysInfo[1][FindColorPitch(keysInfo[1], 0, true)].instrument)).ToArray();
                        solution = Array.IndexOf(differences, differences.Min());
                        break;
                    case 'D':
                        solution = FindColorPitch(keysInfo[2], keysInfo[1][0].pitch, false);
                        break;
                }
                break;
            case 3:
                switch (letter)
                {
                    case 'A':
                        solution = FindColorPitch(keysInfo[3], pressedKeys[2].color, true);
                        break;
                    case 'B':
                        solution = FindColorPitch(keysInfo[3], 3 - keysInfo[2][FindColorPitch(keysInfo[2], 3, true)].pitch, false);
                        break;
                    case 'C':
                        var differences = keysInfo[stage].Take(4).Select(x => Math.Abs(x.instrument - keysInfo[2][FindColorPitch(keysInfo[2], 1, false)].instrument)).ToArray();
                        solution = Array.IndexOf(differences, differences.Min());
                        break;
                    case 'D':
                        solution = FindColorPitch(keysInfo[2], pressedKeys[0].color, true);
                        break;
                }
                break;
            case 4:
                switch (letter)
                {
                    case 'A':
                        solution = FindColorPitch(keysInfo[4], keysInfo[3][FindColorPitch(keysInfo[3], keysInfo[0][FindColorPitch(keysInfo[0], 1, true)].pitch, false)].color, true);
                        break;
                    case 'B':
                        solution = FindColorPitch(keysInfo[3], pressedKeys[2].color, true);
                        break;
                    case 'C':
                        var differences = keysInfo[stage].Take(4).Select(x => Math.Abs(x.instrument - keysInfo[3][1].instrument)).ToArray();
                        solution = Array.IndexOf(differences, differences.Min());
                        break;
                    case 'D':
                        solution = FindColorPitch(keysInfo[3], keysInfo[1][FindColorPitch(keysInfo[1], pressedKeys[2].color, true)].pitch, false);
                        break;
                }
                break;
            case 5:
                module.HandlePass();
                moduleSolved = true;
                Debug.LogFormat("[Melody Memory #{0}] Module solved!", moduleId);
                colorblindText.text = "";
                StartCoroutine(SolveAnimation());
                break;
        }
        if (stage != 5)
            Debug.LogFormat("[Melody Memory #{0}] The key to press is key {1}.", moduleId, solution + 1);
    }

    void PressKey(KMSelectable key)
    {
        key.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, key.transform);
        if (moduleSolved)
            return;
        var ix = Array.IndexOf(keys, key);
        if (audioRef != null)
        {
            audioRef.StopSound();
            audioRef = null;
        }
        audioRef = audio.HandlePlaySoundAtTransformWithRef(instrumentNames[keysInfo[stage][ix].instrument].Replace(" ", "") + (keysInfo[stage][ix].pitch + 1), key.transform, false);
        if (recording)
        {
            Debug.LogFormat("[Melody Memory #{0}] You pressed key {1}.", moduleId, ix + 1);
            if (ix == solution)
            {
                StopAllCoroutines();
                Debug.LogFormat("[Melody Memory #{0}] That was correct.", moduleId);
                pressedKeys.Add(keysInfo[stage][ix]);
                stage++;
                recording = false;
                leds[stage - 1].material.color = Color.green;
            }
            else
            {
                module.HandleStrike();
                Debug.LogFormat("[Melody Memory #{0}] That was incorrect. Strike!", moduleId);
                Debug.LogFormat("[Melody Memory #{0}] Resetting...", moduleId);
                stage = 0;
                recording = false;
                pressedKeys.Clear();
                foreach (Renderer led in leds)
                    led.material.color = Color.black;
            }
            GenerateStage();
        }
    }

    void PressPlayButton()
    {
        playButton.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, playButton.transform);
        if (moduleSolved)
            return;
        if (audioRef != null)
        {
            audioRef.StopSound();
            audioRef = null;
        }
        audioRef = audio.HandlePlaySoundAtTransformWithRef(instrumentNames[keysInfo[stage][4].instrument].Replace(" ", "") + (keysInfo[stage][4].pitch + 1), playButton.transform, false);
    }

    void PressRecordButton()
    {
        playButton.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, recordButton.transform);
        if (moduleSolved)
            return;
        recording = !recording;
        recordButton.GetComponent<Renderer>().material.color = recording ? colors[0] : lightGray;
    }

    IEnumerator ColorKeys()
    {
        var order = Enumerable.Range(0, 4).ToList().Shuffle();
        if (!firstTime)
        {
            for (int i = 0; i < 4; i++)
            {
                StartCoroutine(FadeKey(keys[order[i]].GetComponent<Renderer>(), blankColor));
                yield return new WaitForSeconds(rnd.Range(.5f, .6f));
            }
            if (stage == 5)
                yield break;
            yield return new WaitForSeconds(1f);
        }
        firstTime = false;
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(FadeKey(keys[order[i]].GetComponent<Renderer>(), colors[keysInfo[stage][order[i]].color]));
            yield return new WaitForSeconds(rnd.Range(.5f, .75f));
        }
    }

    IEnumerator FadeKey(Renderer key, Color keyColor)
    {
        var startingColor = key.material.color;
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            key.material.color = Color.Lerp(startingColor, keyColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        key.material.color = keyColor;
    }

    IEnumerator SolveAnimation()
    {
        yield return new WaitForSeconds(.5f);
        for (int i = 0; i < 3; i++)
        {
            foreach (Renderer led in leds)
                led.material.color = Color.black;
            yield return new WaitForSeconds(.1f);
            foreach (Renderer led in leds)
                led.material.color = Color.green;
            yield return new WaitForSeconds(.1f);
        }
        audio.PlaySoundAtTransform("solve", transform);
        for (int i = 0; i < 5; i++)
        {
            leds[i].material.color = Color.black;
            yield return new WaitForSeconds(.1f);
        }
        for (int i = 0; i < 5; i++)
        {
            leds[i].material.color = colors[i];
            yield return new WaitForSeconds(.1f);
        }
    }

    static int FindColorPitch(keyInfo[] info, int index, bool color)
    {
        return Array.IndexOf(info, color ? info.First(x => x.color == index) : info.First(x => x.pitch == index));
    }

    class keyInfo
    {
        public int position { get; set; }
        public int color { get; set; }
        public int pitch { get; set; }
        public int instrument { get; set; }

        public keyInfo(int p, int c, int pt, int i)
        {
            position = p;
            color = c;
            pitch = pt;
            instrument = i;
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} play [Presses the play button] !{0} record [Presses the record button] !{0} <1/2/3/4> [Presses the key in that position]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Replace(" ", "").ToLowerInvariant();
        Debug.Log(input);
        if (input == "play")
        {
            yield return null;
            playButton.OnInteract();
        }
        else if (input == "record")
        {
            yield return null;
            recordButton.OnInteract();
        }
        else if (input.All(c => "1234".Contains(c)))
        {
            yield return null;
            foreach (char c in input)
            {
                keys[int.Parse(c.ToString()) - 1].OnInteract();
                yield return new WaitForSeconds(1f);
            }
        }
        else
            yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (!recording)
            {
                recordButton.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            keys[solution].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
