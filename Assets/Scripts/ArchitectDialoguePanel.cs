using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Esegue a schermo uno scambio di battute tra Vitruvio e l'Imperatore, una alla volta
// (si avanza premendo advanceKey). Non decide da sé quando partire: è MonumentProgressTracker
// a chiamare PlayDialogue() al raggiungimento di una soglia di pezzi piazzati.
//
// Mentre il dialogo è a schermo, IsDialogueActive è true: PlayerPieceInteractor lo controlla
// per mettere in pausa il grab/rilascio dei pezzi, così lo stesso tasto non fa doppio lavoro.
public class ArchitectDialoguePanel : MonoBehaviour
{
    public static ArchitectDialoguePanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private Key advanceKey = Key.E;

    // I due ritratti sono due Image separate e già posizionate in scena (una a sinistra
    // per Vitruvio, una a destra per l'Imperatore, o viceversa): qui non si scambia lo
    // sprite, si evidenzia semplicemente chi sta parlando schiarendo la sua Image e
    // scurendo l'altra.
    [Header("Ritratti")]
    [SerializeField] private Image vitruvioPortraitImage;
    [SerializeField] private Image imperatorePortraitImage;
    [SerializeField] private Color activeSpeakerColor = Color.white;
    [SerializeField] private Color inactiveSpeakerColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private float speakerTransitionDuration = 0.3f;
    [SerializeField] private Ease speakerTransitionEase = Ease.Linear;

    private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
    private Tween vitruvioColorTween;
    private Tween imperatoreColorTween;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    public void PlayDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        queue.Clear();
        foreach (DialogueLine line in lines)
        {
            queue.Enqueue(line);
        }

        IsDialogueActive = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (queue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = queue.Dequeue();

        if (speakerText != null)
        {
            speakerText.text = line.speaker.ToString();
        }

        if (lineText != null)
        {
            lineText.text = line.text;
        }

        SetActiveSpeaker(line.speaker);
    }

    private void SetActiveSpeaker(Speaker speaker)
    {
        if (vitruvioPortraitImage != null)
        {
            Color target = speaker == Speaker.Vitruvio ? activeSpeakerColor : inactiveSpeakerColor;
            vitruvioColorTween?.Kill();
            vitruvioColorTween = vitruvioPortraitImage.DOColor(target, speakerTransitionDuration).SetEase(speakerTransitionEase);
        }

        if (imperatorePortraitImage != null)
        {
            Color target = speaker == Speaker.Imperatore ? activeSpeakerColor : inactiveSpeakerColor;
            imperatoreColorTween?.Kill();
            imperatoreColorTween = imperatorePortraitImage.DOColor(target, speakerTransitionDuration).SetEase(speakerTransitionEase);
        }
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
