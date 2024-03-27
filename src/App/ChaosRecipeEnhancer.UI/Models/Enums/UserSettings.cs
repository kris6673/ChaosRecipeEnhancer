﻿namespace ChaosRecipeEnhancer.UI.Models.Enums;

public enum ConnectionStatusTypes
{
    ConnectionNotValidated,
    ValidatedConnection,
    ConnectionError,
    AttemptingLogin
}

public enum StashTabOverlayHighlightMode
{
    SetBySet,
    ItemByItem
}

public enum StashTabQueryMode
{
    SelectTabsFromList,
    TabNamePrefix
}

public enum SetTrackerOverlayMode
{
    Standard,
    VerticalStandard,
    Minified,
    VerticalMinified,
    OnlyButtons,
    OnlyButtonsVertical,
    OnlyMinifiedButtons,
    OnlyMinifiedButtonsVertical
}

public enum SetTrackerOverlayItemCounterDisplayMode
{
    None,
    TotalItems,
    ItemsMissing
}

public enum ClientLogFileLocationMode
{
    DefaultStandaloneLocation,
    DefaultSteamLocation,
    CustomLocation
}