using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Строка списка вопросов: номер, текст, отметка о решении.</summary>
    public class QuestionItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text numberText;
        [SerializeField] private Text questionText;

        public CrosswordQuestion Question { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public event Action<QuestionItemUI> Clicked;

        private bool _active;
        private Coroutine _routine;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Bind(CrosswordQuestion question)
        {
            Question = question;
            _active = false;
            background.color = UIPalette.QuestionNormal;
            Refresh();
        }

        public void Refresh()
        {
            numberText.text = Question.Number.ToString();
            if (Question.IsSolved)
            {
                string green = ColorUtility.ToHtmlStringRGB(UIPalette.CellLetterCorrect);
                questionText.text = $"{Question.Text} — <color=#{green}><b>{Question.Answer}</b></color>";
                questionText.color = UIPalette.TextSecondary;
                numberText.color = UIPalette.Success;
            }
            else
            {
                questionText.text = Question.Text;
                questionText.color = UIPalette.TextPrimary;
                numberText.color = UIPalette.Accent;
            }
        }

        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            Color target = active ? UIPalette.QuestionActive : UIPalette.QuestionNormal;
            if (isActiveAndEnabled)
                AnimationController.Restart(this, ref _routine, AnimationController.ColorTo(background, target, 0.15f));
            else
                background.color = target;
        }

        public void PlaySolved()
        {
            Refresh();
            if (isActiveAndEnabled)
                AnimationController.Restart(this, ref _routine, SolvedRoutine());
        }

        private IEnumerator SolvedRoutine()
        {
            Color solvedColor = UIPalette.CellCorrect;
            background.color = solvedColor;
            yield return AnimationController.Pulse(numberText.transform, 0.3f, 1.3f);
            yield return AnimationController.ColorTo(background, _active ? UIPalette.QuestionActive : UIPalette.QuestionNormal, 0.4f);
        }

        private void OnDisable()
        {
            _routine = null;
            numberText.transform.localScale = Vector3.one;
        }
    }
}
