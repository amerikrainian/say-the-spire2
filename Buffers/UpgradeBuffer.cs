using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
namespace SayTheSpire2.Buffers;

public class UpgradeBuffer : Buffer
{
    private CardModel? _model;
    private CardModel? _previewModel;
    private bool _forceUnavailable;

    public UpgradeBuffer() : base("upgrade") { }

    private static string NoUpgradeText()
    {
        return LocalizationManager.GetOrDefault("ui", "CARD.UPGRADE_UNAVAILABLE", "No upgrade available");
    }

    public void Bind(CardModel model)
    {
        _model = model;
        _previewModel = null;
        _forceUnavailable = false;
    }

    public void Bind(CardModel model, CardModel? previewModel)
    {
        _model = model;
        _previewModel = previewModel;
        _forceUnavailable = false;
    }

    public void BindUnavailable()
    {
        _model = null;
        _previewModel = null;
        _forceUnavailable = true;
    }

    protected override void ClearBinding()
    {
        _model = null;
        _previewModel = null;
        _forceUnavailable = false;
        Clear();
    }

    public override void Update()
    {
        if (_forceUnavailable)
        {
            Repopulate(() => Add(NoUpgradeText()));
            return;
        }

        if (_model == null && _previewModel == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        if (_previewModel != null)
        {
            CardBuffer.Populate(this, _previewModel);
            return;
        }

        var model = _model;
        if (model == null) return;

        if (!model.IsUpgradable)
        {
            Add(NoUpgradeText());
            return;
        }

        // Beta 2026-04-23: CardScope can be a NullRunState sentinel instead of
        // null. Calling CloneCard on it throws, so treat both as "no scope".
        var cardScope = model.CardScope;
        if (cardScope == null || cardScope is NullRunState)
        {
            try
            {
                var clone = (CardModel)model.MutableClone();
                clone.UpgradeInternal();
                CardBuffer.Populate(this, clone);
                return;
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Card upgrade preview clone fallback failed: {e.Message}");
                Add(NoUpgradeText());
                return;
            }
        }

        try
        {
            var clone = cardScope.CloneCard(model);
            clone.UpgradeInternal();

            Add(clone.Title);
            var typeRarity = clone.Type.ToString();
            if (clone.Rarity != CardRarity.Common)
                typeRarity += $", {clone.Rarity}";
            Add(typeRarity);

            if (clone.EnergyCost != null)
            {
                if (clone.EnergyCost.CostsX)
                    Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_ENERGY", "X energy"));
                else
                    Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = clone.EnergyCost.GetWithModifiers(CostModifiers.All) }).Resolve());
            }

            if (clone.CurrentStarCost > 0)
                Add($"{clone.CurrentStarCost}");

            try
            {
                var desc = clone.GetDescriptionForUpgradePreview();
                if (!string.IsNullOrEmpty(desc))
                    Add(desc);
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Upgrade description access failed: {e.Message}"); }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade preview failed: {e.Message}");
            Add(NoUpgradeText());
        }
    }
}
