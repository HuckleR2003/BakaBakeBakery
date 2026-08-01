using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace BakaBakeBakery.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BakeryHudController : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;

        private readonly List<Button> recipeCards = new();
        private UIDocument document;
        private VisualElement ledger;
        private Button ledgerButton;

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            var root = document.rootVisualElement;
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            recipeCards.Clear();
            recipeCards.AddRange(root.Query<Button>(className: "recipe-card").ToList());
            foreach (var card in recipeCards)
            {
                card.RegisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            ledger = root.Q<VisualElement>("upgrade-ledger");
            ledgerButton = root.Q<Button>("ledger-button");
            if (ledgerButton != null)
            {
                ledgerButton.clicked += ToggleLedger;
            }
        }

        private void OnDisable()
        {
            foreach (var card in recipeCards)
            {
                card.UnregisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            if (ledgerButton != null)
            {
                ledgerButton.clicked -= ToggleLedger;
            }

            recipeCards.Clear();
        }

        private void OnRecipeCardClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button selected)
            {
                return;
            }

            if (selected.ClassListContains("recipe-card--locked"))
            {
                return;
            }

            foreach (var card in recipeCards)
            {
                card.EnableInClassList("recipe-card--selected", card == selected);
            }
        }

        private void ToggleLedger()
        {
            ledger?.ToggleInClassList("ledger--closed");
        }

        private void Update()
        {
            if (ledger != null
                && !ledger.ClassListContains("ledger--closed")
                && Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ledger.AddToClassList("ledger--closed");
            }
        }
    }
}
