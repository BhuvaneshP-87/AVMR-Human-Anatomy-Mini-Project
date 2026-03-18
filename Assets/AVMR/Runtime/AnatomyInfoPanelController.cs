using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AVMR.AnatomyViewer
{
    public class AnatomyInfoPanelController : MonoBehaviour
    {
        [Serializable]
        struct NarrationEntry
        {
            public string title;
            public string body;
            public AudioClip clip;
        }

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] AudioSource audioSource;
        [SerializeField] List<NarrationEntry> narrations = new();

        public void UpdatePanel(string title, string body)
        {
            if (titleText != null)
                titleText.text = title;

            if (bodyText != null)
                bodyText.text = body;

            PlayNarration(title, body);
        }

        void PlayNarration(string title, string body)
        {
            if (audioSource == null)
                return;

            foreach (var narration in narrations)
            {
                if (narration.title != title || narration.body != body || narration.clip == null)
                    continue;

                audioSource.clip = narration.clip;
                audioSource.Play();
                return;
            }
        }
    }
}
