namespace WorldofGoses.Domain;

/// <summary>
/// Translation keys for the project's translatable strings. The
/// domain returns these keys; the UI resolves them via
/// <c>LocaleManager.Translate</c> (which delegates to
/// <c>TranslationServer.Translate</c>). Keys are stable: a rename
/// here is a build-time error, not a silent translation loss.
///
/// <para>
/// The keys are organized by surface (Narrative, Ui). Future
/// surfaces (Chronicle, Notifications, Tooltips) get their own
/// nested class so the catalog stays greppable.
/// </para>
///
/// <para>
/// This file does not depend on Godot, on purpose: AGENTS.md §8
/// keeps the domain pure. The UI layer is the only consumer of
/// <see cref="TranslationServer"/>.
/// </para>
/// </summary>
public static class Tr
{
    public static class Narrative
    {
        // 12 questions × (1 title + 1 body + 4-6 option{label,consequence})
        // = ~70 keys.

        // Q("hand", ...)
        public const string HandTitle = "narrative.hand.title";
        public const string HandBody = "narrative.hand.body";
        public const string HandOptionHoldLabel = "narrative.hand.option.hold.label";
        public const string HandOptionHoldConsequence = "narrative.hand.option.hold.consequence";
        public const string HandOptionObserveLabel = "narrative.hand.option.observe.label";
        public const string HandOptionObserveConsequence = "narrative.hand.option.observe.consequence";
        public const string HandOptionStabiliseLabel = "narrative.hand.option.stabilise.label";
        public const string HandOptionStabiliseConsequence = "narrative.hand.option.stabilise.consequence";
        public const string HandOptionCallLabel = "narrative.hand.option.call.label";
        public const string HandOptionCallConsequence = "narrative.hand.option.call.consequence";

        // Q("word", ...)
        public const string WordTitle = "narrative.word.title";
        public const string WordBody = "narrative.word.body";
        public const string WordOptionFindLabel = "narrative.word.option.find.label";
        public const string WordOptionFindConsequence = "narrative.word.option.find.consequence";
        public const string WordOptionReturnLabel = "narrative.word.option.return.label";
        public const string WordOptionReturnConsequence = "narrative.word.option.return.consequence";
        public const string WordOptionRememberLabel = "narrative.word.option.remember.label";
        public const string WordOptionRememberConsequence = "narrative.word.option.remember.consequence";
        public const string WordOptionContinueLabel = "narrative.word.option.continue.label";
        public const string WordOptionContinueConsequence = "narrative.word.option.continue.consequence";

        // Q("detail", ...)
        public const string DetailTitle = "narrative.detail.title";
        public const string DetailBody = "narrative.detail.body";
        public const string DetailOptionNameLabel = "narrative.detail.option.name.label";
        public const string DetailOptionNameConsequence = "narrative.detail.option.name.consequence";
        public const string DetailOptionHandsLabel = "narrative.detail.option.hands.label";
        public const string DetailOptionHandsConsequence = "narrative.detail.option.hands.consequence";
        public const string DetailOptionObjectLabel = "narrative.detail.option.object.label";
        public const string DetailOptionObjectConsequence = "narrative.detail.option.object.consequence";
        public const string DetailOptionJourneyLabel = "narrative.detail.option.journey.label";
        public const string DetailOptionJourneyConsequence = "narrative.detail.option.journey.consequence";

        // Q("old-form", ...)
        public const string OldFormTitle = "narrative.old-form.title";
        public const string OldFormBody = "narrative.old-form.body";
        public const string OldFormOptionExactLabel = "narrative.old-form.option.exact.label";
        public const string OldFormOptionExactConsequence = "narrative.old-form.option.exact.consequence";
        public const string OldFormOptionSensationLabel = "narrative.old-form.option.sensation.label";
        public const string OldFormOptionSensationConsequence = "narrative.old-form.option.sensation.consequence";
        public const string OldFormOptionSeparateLabel = "narrative.old-form.option.separate.label";
        public const string OldFormOptionSeparateConsequence = "narrative.old-form.option.separate.consequence";
        public const string OldFormOptionReleaseLabel = "narrative.old-form.option.release.label";
        public const string OldFormOptionReleaseConsequence = "narrative.old-form.option.release.consequence";

        // Q("time", ...)
        public const string TimeTitle = "narrative.time.title";
        public const string TimeBody = "narrative.time.body";
        public const string TimeOptionCauseLabel = "narrative.time.option.cause.label";
        public const string TimeOptionCauseConsequence = "narrative.time.option.cause.consequence";
        public const string TimeOptionFeelingLabel = "narrative.time.option.feeling.label";
        public const string TimeOptionFeelingConsequence = "narrative.time.option.feeling.consequence";
        public const string TimeOptionPromisesLabel = "narrative.time.option.promises.label";
        public const string TimeOptionPromisesConsequence = "narrative.time.option.promises.consequence";
        public const string TimeOptionPlacesLabel = "narrative.time.option.places.label";
        public const string TimeOptionPlacesConsequence = "narrative.time.option.places.consequence";

        // Q("mortality", ...)
        public const string MortalityTitle = "narrative.mortality.title";
        public const string MortalityBody = "narrative.mortality.body";
        public const string MortalityOptionWeightLabel = "narrative.mortality.option.weight.label";
        public const string MortalityOptionWeightConsequence = "narrative.mortality.option.weight.consequence";
        public const string MortalityOptionUnderstandLabel = "narrative.mortality.option.understand.label";
        public const string MortalityOptionUnderstandConsequence = "narrative.mortality.option.understand.consequence";
        public const string MortalityOptionOthersLabel = "narrative.mortality.option.others.label";
        public const string MortalityOptionOthersConsequence = "narrative.mortality.option.others.consequence";
        public const string MortalityOptionNewLabel = "narrative.mortality.option.new.label";
        public const string MortalityOptionNewConsequence = "narrative.mortality.option.new.consequence";

        // Q("world", ...)
        public const string WorldTitle = "narrative.world.title";
        public const string WorldBody = "narrative.world.body";
        public const string WorldOptionEarthLabel = "narrative.world.option.earth.label";
        public const string WorldOptionEarthConsequence = "narrative.world.option.earth.consequence";
        public const string WorldOptionWaterLabel = "narrative.world.option.water.label";
        public const string WorldOptionWaterConsequence = "narrative.world.option.water.consequence";
        public const string WorldOptionFireLabel = "narrative.world.option.fire.label";
        public const string WorldOptionFireConsequence = "narrative.world.option.fire.consequence";
        public const string WorldOptionAirLabel = "narrative.world.option.air.label";
        public const string WorldOptionAirConsequence = "narrative.world.option.air.consequence";
        public const string WorldOptionAetherLabel = "narrative.world.option.aether.label";
        public const string WorldOptionAetherConsequence = "narrative.world.option.aether.consequence";
        public const string WorldOptionNoneLabel = "narrative.world.option.none.label";
        public const string WorldOptionNoneConsequence = "narrative.world.option.none.consequence";

        // Q("perception", ...)
        public const string PerceptionTitle = "narrative.perception.title";
        public const string PerceptionBody = "narrative.perception.body";
        public const string PerceptionOptionArdhenLabel = "narrative.perception.option.ardhen.label";
        public const string PerceptionOptionArdhenConsequence = "narrative.perception.option.ardhen.consequence";
        public const string PerceptionOptionEiruneLabel = "narrative.perception.option.eirune.label";
        public const string PerceptionOptionEiruneConsequence = "narrative.perception.option.eirune.consequence";
        public const string PerceptionOptionKovariLabel = "narrative.perception.option.kovari.label";
        public const string PerceptionOptionKovariConsequence = "narrative.perception.option.kovari.consequence";
        public const string PerceptionOptionMyrvenLabel = "narrative.perception.option.myrven.label";
        public const string PerceptionOptionMyrvenConsequence = "narrative.perception.option.myrven.consequence";

        // Q("orientation", ...)
        public const string OrientationTitle = "narrative.orientation.title";
        public const string OrientationBody = "narrative.orientation.body";
        public const string OrientationOptionVaelunLabel = "narrative.orientation.option.vaelun.label";
        public const string OrientationOptionVaelunConsequence = "narrative.orientation.option.vaelun.consequence";
        public const string OrientationOptionOrvethLabel = "narrative.orientation.option.orveth.label";
        public const string OrientationOptionOrvethConsequence = "narrative.orientation.option.orveth.consequence";
        public const string OrientationOptionCaelithLabel = "narrative.orientation.option.caelith.label";
        public const string OrientationOptionCaelithConsequence = "narrative.orientation.option.caelith.consequence";
        public const string OrientationOptionTherynLabel = "narrative.orientation.option.theryn.label";
        public const string OrientationOptionTherynConsequence = "narrative.orientation.option.theryn.consequence";

        // Q("threshold", ...)
        public const string ThresholdTitle = "narrative.threshold.title";
        public const string ThresholdBody = "narrative.threshold.body";
        public const string ThresholdOptionSupportLabel = "narrative.threshold.option.support.label";
        public const string ThresholdOptionSupportConsequence = "narrative.threshold.option.support.consequence";
        public const string ThresholdOptionControlLabel = "narrative.threshold.option.control.label";
        public const string ThresholdOptionControlConsequence = "narrative.threshold.option.control.consequence";
        public const string ThresholdOptionMobilityLabel = "narrative.threshold.option.mobility.label";
        public const string ThresholdOptionMobilityConsequence = "narrative.threshold.option.mobility.consequence";
        public const string ThresholdOptionPrecisionLabel = "narrative.threshold.option.precision.label";
        public const string ThresholdOptionPrecisionConsequence = "narrative.threshold.option.precision.consequence";
        public const string ThresholdOptionAssaultLabel = "narrative.threshold.option.assault.label";
        public const string ThresholdOptionAssaultConsequence = "narrative.threshold.option.assault.consequence";

        // Q("ground", ...)
        public const string GroundTitle = "narrative.ground.title";
        public const string GroundBody = "narrative.ground.body";
        public const string GroundOptionClarityLabel = "narrative.ground.option.clarity.label";
        public const string GroundOptionClarityConsequence = "narrative.ground.option.clarity.consequence";
        public const string GroundOptionSharedLabel = "narrative.ground.option.shared.label";
        public const string GroundOptionSharedConsequence = "narrative.ground.option.shared.consequence";
        public const string GroundOptionMoveLabel = "narrative.ground.option.move.label";
        public const string GroundOptionMoveConsequence = "narrative.ground.option.move.consequence";
        public const string GroundOptionMarkLabel = "narrative.ground.option.mark.label";
        public const string GroundOptionMarkConsequence = "narrative.ground.option.mark.consequence";

        // Q("unchanged", ...)
        public const string UnchangedTitle = "narrative.unchanged.title";
        public const string UnchangedBody = "narrative.unchanged.body";
        public const string UnchangedOptionProtectLabel = "narrative.unchanged.option.protect.label";
        public const string UnchangedOptionProtectConsequence = "narrative.unchanged.option.protect.consequence";
        public const string UnchangedOptionFreedomLabel = "narrative.unchanged.option.freedom.label";
        public const string UnchangedOptionFreedomConsequence = "narrative.unchanged.option.freedom.consequence";
        public const string UnchangedOptionUnderstandLabel = "narrative.unchanged.option.understand.label";
        public const string UnchangedOptionUnderstandConsequence = "narrative.unchanged.option.understand.consequence";
        public const string UnchangedOptionPathsLabel = "narrative.unchanged.option.paths.label";
        public const string UnchangedOptionPathsConsequence = "narrative.unchanged.option.paths.consequence";
    }

    public static class Ui
    {
        /// <summary>Tooltip on the language switcher in the pause menu.</summary>
        public const string LanguageButtonTooltip = "ui.common.language.tooltip";

        /// <summary>Label of the language switcher when the current locale is English.</summary>
        public const string LanguageButtonLabelEnglish = "ui.common.language.english";

        /// <summary>Label of the language switcher when the current locale is Spanish.</summary>
        public const string LanguageButtonLabelSpanish = "ui.common.language.spanish";
    }
}
